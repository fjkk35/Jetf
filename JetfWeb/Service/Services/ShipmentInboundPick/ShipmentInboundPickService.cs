using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.ShipmentInboundPick.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace Service.Services.ShipmentInboundPick
{
    public class ShipmentInboundPickService : _BaseService
    {
        public List<ShipmentInboundPickModel> GetData(ShipmentInboundPickRequest request)
        {
            using (var db = CreateJetfDbContext())
            {
                var query = db.ShipmentInbounds
                    .AsNoTracking()
                    .Where(x => x.ProcessType != ShipmentInboundProcessType.TempData)
                    .Where(x => !x.WarehouseProcessType.HasValue);

                if (!string.IsNullOrWhiteSpace(request.ProcessTimeStart)
                    && DateTime.TryParse(request.ProcessTimeStart, out var startDate))
                {
                    query = query.WhereIf(true, x => x.ProcessTime >= startDate);
                }

                if (!string.IsNullOrWhiteSpace(request.ProcessTimeEnd)
                    && DateTime.TryParse(request.ProcessTimeEnd, out var endDate))
                {
                    var processTimeEnd = endDate.AddDays(1);
                    query = query.WhereIf(true, x => x.ProcessTime < processTimeEnd);
                }

                var custCodes = request.CustCodes?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct()
                    .ToList();

                query = query.WhereIf(custCodes?.Any() == true, x => custCodes.Contains(x.CustCode));

                query = query.WhereIf(request.ProcessType.HasValue, x => x.ProcessType == request.ProcessType);

                var data = query
                    .OrderBy(x => x.LocationCode)
                    .Select(x => new ShipmentInboundPickModel
                    {
                        TrackingNo = x.TrackingNo,
                        SeqNo = x.SeqNo,
                        LocationCode = x.LocationCode,
                        ProcessType = (ShipmentInboundProcessType)(x.ProcessType ?? 0),
                        ProcessImporter = x.ProcessImporter,
                        ProcessImporterPhone = x.ProcessImporterPhone,
                        ProcessImporterAddr = x.ProcessImporterAddr,
                        StoreCode = x.StoreCode,
                        StoreName = x.StoreName,
                        Tax = x.Tax ?? 0,
                        Ccfee = x.Ccfee ?? 0,
                        Cod = x.Cod ?? 0,
                        Fee = x.Fee ?? 0,
                        FreightFee = (int)(x.FreightFee ?? 0),
                        CustCode = x.CustCode,
                        DataType = x.DataType,
                        ProcessTransNo = (ShipmentInboundProcessTransNo)(x.ProcessTransNo ?? 0),
                        Remark = x.Remark
                    })
                    .ToList();

                FillCustomerNames(data);
                return data;
            }
        }

        private void FillCustomerNames(List<ShipmentInboundPickModel> data)
        {
            var airCustCodes = data.Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.CustCode))
                                   .Select(x => x.CustCode)
                                   .Distinct()
                                   .ToList();

            var seaCustCodes = data.Where(x => x.DataType == "海運" && !string.IsNullOrWhiteSpace(x.CustCode))
                                   .Select(x => x.CustCode)
                                   .Distinct()
                                   .ToList();

            var airCustNames = GetAirCustomerNames(airCustCodes);
            var seaCustNames = GetSeaCustomerNames(seaCustCodes);

            foreach (var item in data)
            {
                if (!string.IsNullOrWhiteSpace(item.CustCode))
                {
                    if (item.DataType == "空運" && airCustNames.ContainsKey(item.CustCode))
                    {
                        item.CustName = airCustNames[item.CustCode];
                    }
                    else if (item.DataType == "海運" && seaCustNames.ContainsKey(item.CustCode))
                    {
                        item.CustName = seaCustNames[item.CustCode];
                    }
                }
            }
        }

        public byte[] ExportToExcel(List<ShipmentInboundPickModel> data)
        {
            IWorkbook workbook = new XSSFWorkbook();

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            //檢貨明細
            CreatePickSheet(workbook, data, headerStyle, dataStyle);
            //新竹
            CreateHctSheet(workbook, data, headerStyle, dataStyle);
            //黑貓
            CreateTCatSheet(workbook, data, headerStyle, dataStyle);
            //超商7-11
            CreateSevenElevenSheet(workbook, data, headerStyle, dataStyle);
            //郵局
            CreatePostSheet(workbook, data, headerStyle, dataStyle);

            using (var ms = new MemoryStream())
            {
                workbook.Write(ms);
                return ms.ToArray();
            }
        }

        private void CreatePickSheet(IWorkbook workbook, List<ShipmentInboundPickModel> data, ICellStyle headerStyle, ICellStyle dataStyle)
        {
            ISheet sheet = workbook.CreateSheet("撿貨明細");

            IRow headerRow = sheet.CreateRow(0);
            var headers = new List<string> { "單號", "客戶", "流水號", "儲位", "處理方式", "備註" };
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            int rowIndex = 1;
            foreach (var item in data)
            {
                IRow dataRow = sheet.CreateRow(rowIndex);
                NpoiCell.CreateCell(dataRow, 0, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(dataRow, 1, item.CustName, dataStyle);
                NpoiCell.CreateCell(dataRow, 2, item.SeqNo, dataStyle);
                NpoiCell.CreateCell(dataRow, 3, item.LocationCode, dataStyle);
                NpoiCell.CreateCell(dataRow, 4, item.ProcessTypeName, dataStyle);
                NpoiCell.CreateCell(dataRow, 5, item.Remark, dataStyle);
                rowIndex++;
            }

            //AutoSize+20字元
            sheet.AutoSizeColumns(headers.Count, scale: 1, minWidth: 20);
        }

        /// <summary>
        /// 派件公司格式(新竹)
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="data"></param>
        /// <param name="headerStyle"></param>
        /// <param name="dataStyle"></param>
        private void CreateHctSheet(IWorkbook workbook, List<ShipmentInboundPickModel> data, ICellStyle headerStyle, ICellStyle dataStyle)
        {
            ISheet sheet = workbook.CreateSheet("派件公司格式(新竹)");

            var filteredData = data.Where(x => x.ProcessType == EnumTax.ShipmentInboundProcessType.NewTrackingNo
                                            && x.ProcessTransNo == EnumTax.ShipmentInboundProcessTransNo.Hct).ToList();

            IRow headerRow = sheet.CreateRow(0);
            var headers = new List<string>
            {
                "序號", "訂單號", "收件人姓名", "收件人地址", "收件人電話",
                "託運備註中文限制", "商品別編號", "商品數量", "才積/重量/總長(30/60/90/120..)",
                "代收款總金額", "指定配送日期", "指定配送時間", "客戶"
            };
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            int rowIndex = 1;
            int seqNo = 1;
            foreach (var item in filteredData)
            {
                IRow dataRow = sheet.CreateRow(rowIndex);
                NpoiCell.CreateIntCell(dataRow, 0, seqNo, dataStyle);
                NpoiCell.CreateCell(dataRow, 1, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(dataRow, 2, item.ProcessImporter, dataStyle);
                NpoiCell.CreateCell(dataRow, 3, item.ProcessImporterAddr, dataStyle);
                NpoiCell.CreateCell(dataRow, 4, item.ProcessImporterPhone, dataStyle);
                NpoiCell.CreateCell(dataRow, 5, BuildHctRemark(item), dataStyle);
                NpoiCell.CreateCell(dataRow, 6, "", dataStyle);
                NpoiCell.CreateIntCell(dataRow, 7, 1, dataStyle);
                NpoiCell.CreateIntCell(dataRow, 8, 5, dataStyle);
                NpoiCell.CreateIntCell(dataRow, 9, item.TotalAmount, dataStyle);
                NpoiCell.CreateCell(dataRow, 10, "", dataStyle);
                NpoiCell.CreateCell(dataRow, 11, "", dataStyle);
                NpoiCell.CreateCell(dataRow, 12, item.CustName, dataStyle);
                rowIndex++;
                seqNo++;
            }

            var fixedWidths = new Dictionary<int, int>
            {
                { 9, 12000 }
            };

            //AutoSize+20字元
            sheet.AutoSizeColumns(headers.Count, fixedWidths, scale: 1, minWidth: 20);
        }

        /// <summary>
        /// 派件公司格式(黑貓)
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="data"></param>
        /// <param name="headerStyle"></param>
        /// <param name="dataStyle"></param>
        private void CreateTCatSheet(IWorkbook workbook, List<ShipmentInboundPickModel> data, ICellStyle headerStyle, ICellStyle dataStyle)
        {
            ISheet sheet = workbook.CreateSheet("派件公司格式(黑貓)");

            var filteredData = data.Where(x => x.ProcessType == EnumTax.ShipmentInboundProcessType.NewTrackingNo
                                            && x.ProcessTransNo == EnumTax.ShipmentInboundProcessTransNo.TCat).ToList();

            IRow headerRow = sheet.CreateRow(0);
            var headers = new List<string>
            {
                "出貨日期", "訂單編號", "收件人姓名", "收件人地址", "收件人電話",
                "備註", "託運單號", "預定配達日", "配達時段", "品名",
                "代收款總金額", "契客代號", "溫層", "尺寸", "寄件人姓名",
                "寄件人地址", "寄件人電話"
            };
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            int rowIndex = 1;
            string today = DateTime.Now.ToString("yyyyMMdd");
            foreach (var item in filteredData)
            {
                var senderInfo = GetTCatSenderInfo(item.CustCode);

                IRow dataRow = sheet.CreateRow(rowIndex);
                NpoiCell.CreateCell(dataRow, 0, today, dataStyle);
                NpoiCell.CreateCell(dataRow, 1, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(dataRow, 2, item.ProcessImporter, dataStyle);
                NpoiCell.CreateCell(dataRow, 3, item.ProcessImporterAddr, dataStyle);
                NpoiCell.CreateCell(dataRow, 4, item.ProcessImporterPhone, dataStyle);
                NpoiCell.CreateCell(dataRow, 5, item.Remark, dataStyle);
                NpoiCell.CreateCell(dataRow, 6, "", dataStyle);
                NpoiCell.CreateCell(dataRow, 7, "", dataStyle);
                NpoiCell.CreateCell(dataRow, 8, "", dataStyle);
                NpoiCell.CreateCell(dataRow, 9, "0015", dataStyle);
                NpoiCell.CreateIntCell(dataRow, 10, item.TotalAmount, dataStyle);
                NpoiCell.CreateCell(dataRow, 11, senderInfo.ContractCode, dataStyle);
                NpoiCell.CreateIntCell(dataRow, 12, 1, dataStyle);
                NpoiCell.CreateIntCell(dataRow, 13, 1, dataStyle);
                NpoiCell.CreateCell(dataRow, 14, senderInfo.SenderName, dataStyle);
                NpoiCell.CreateCell(dataRow, 15, "桃園市蘆竹區南山路二段122號", dataStyle);
                NpoiCell.CreateCell(dataRow, 16, senderInfo.SenderPhone, dataStyle);
                rowIndex++;
            }



            //AutoSize+20字元
            sheet.AutoSizeColumns(headers.Count, scale: 1, minWidth: 20);
        }

        private void CreateSevenElevenSheet(IWorkbook workbook, List<ShipmentInboundPickModel> data, ICellStyle headerStyle, ICellStyle dataStyle)
        {
            ISheet sheet = workbook.CreateSheet("派件公司格式(超商)");

            var filteredData = data.Where(x => x.ProcessType == EnumTax.ShipmentInboundProcessType.NewTrackingNo
                                            && x.ProcessTransNo == EnumTax.ShipmentInboundProcessTransNo.SevenEleven).ToList();

            IRow headerRow = sheet.CreateRow(0);
            var headers = new List<string>
            {
                "單號", "收件人姓名", "收件人地址", "收件人電話",
                "門市店號", "門市名稱", "代收款總金額"
            };
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            int rowIndex = 1;
            foreach (var item in filteredData)
            {
                IRow dataRow = sheet.CreateRow(rowIndex);
                NpoiCell.CreateCell(dataRow, 0, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(dataRow, 1, item.ProcessImporter, dataStyle);
                NpoiCell.CreateCell(dataRow, 2, item.ProcessImporterAddr, dataStyle);
                NpoiCell.CreateCell(dataRow, 3, item.ProcessImporterPhone, dataStyle);
                NpoiCell.CreateCell(dataRow, 4, item.StoreCode, dataStyle);
                NpoiCell.CreateCell(dataRow, 5, item.StoreName, dataStyle);
                NpoiCell.CreateIntCell(dataRow, 6, item.TotalAmount, dataStyle);
                rowIndex++;
            }

            //AutoSize+20字元
            sheet.AutoSizeColumns(headers.Count, scale: 1, minWidth: 20);
        }

        private void CreatePostSheet(IWorkbook workbook, List<ShipmentInboundPickModel> data, ICellStyle headerStyle, ICellStyle dataStyle)
        {
            ISheet sheet = workbook.CreateSheet("派件公司格式(郵局)");

            var filteredData = data.Where(x => x.ProcessType == EnumTax.ShipmentInboundProcessType.NewTrackingNo
                                            && x.ProcessTransNo == EnumTax.ShipmentInboundProcessTransNo.Post).ToList();

            IRow headerRow = sheet.CreateRow(0);
            var headers = new List<string>
            {
                "單號", "收件人姓名", "收件人地址", "收件人電話", "代收款總金額"
            };
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            int rowIndex = 1;
            foreach (var item in filteredData)
            {
                IRow dataRow = sheet.CreateRow(rowIndex);
                NpoiCell.CreateCell(dataRow, 0, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(dataRow, 1, item.ProcessImporter, dataStyle);
                NpoiCell.CreateCell(dataRow, 2, item.ProcessImporterAddr, dataStyle);
                NpoiCell.CreateCell(dataRow, 3, item.ProcessImporterPhone, dataStyle);
                NpoiCell.CreateIntCell(dataRow, 4, item.TotalAmount, dataStyle);
                rowIndex++;
            }

            for (int i = 0; i < headers.Count; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }
        }

        private TCatSenderInfo GetTCatSenderInfo(string custCode)
        {
            if (custCode == "00054")
            {
                return new TCatSenderInfo
                {
                    ContractCode = "2495175204",
                    SenderName = "UF集運(菜鳥電商數位物流)",
                    SenderPhone = "02-2736-7699"
                };
            }

            return new TCatSenderInfo
            {
                ContractCode = "2495175202",
                SenderName = "捷豐國際物流股份有限公司",
                SenderPhone = "03-2522568"
            };
        }

        private string BuildHctRemark(ShipmentInboundPickModel item)
        {
            var seqNo = item?.SeqNo ?? string.Empty;
            var locationCode = item?.LocationCode ?? string.Empty;
            return $"{seqNo}_{locationCode}";
        }

        private class TCatSenderInfo
        {
            public string ContractCode { get; set; }
            public string SenderName { get; set; }
            public string SenderPhone { get; set; }
        }
    }
}
