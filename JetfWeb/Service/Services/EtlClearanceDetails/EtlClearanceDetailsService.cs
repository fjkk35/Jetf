using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.Util;
using NPOI.XSSF.UserModel;
using Service.Models;
using Service.Extensions;
using Service.Services.EtlClearanceDetails.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Service.Services.EtlClearanceDetails
{
    public class EtlClearanceDetailsService : _BaseService
    {
        /// <summary>
        /// 取得空快清關明細
        /// </summary>
        /// <returns></returns>
        public List<ManifestModel> GetOrder_Manifest(string sDate, string eDate, string dataTime)
        {
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM [DATA_CENTER].[dbo].[ORDER_MANIFEST] ");
            if (dataTime == "CrtDateTime")
            {
                sb.Append("WHERE CrtDateTime between @SDate and @EDate ");
            }
            else
            {
                sb.Append("WHERE EditDateTime between @SDate and @EDate ");
            }
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.CommandTimeout = 600;
                da.SelectCommand.Parameters.Add("@SDate", SqlDbType.NVarChar).Value = $"{sDate} :00";
                da.SelectCommand.Parameters.Add("@EDate", SqlDbType.NVarChar).Value = $"{eDate} :59";
                da.Fill(dt);
            }
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows[i]["MawbNo"].ToString() == "")
                {
                    dt.Rows[i]["MawbNo"] = "";
                }
            }

            var result = dt.AsEnumerable().Select(t => new ManifestModel()
            {
                SendId = t["SendId"].ToString(),
                CreateDate = t["CreateDate"].ToString(),
                BrokerCode = t["BrokerCode"].ToString(),
                MawbNo = t["MawbNo"].ToString(),
                MainHawbNo = t["MainHawbNo"].ToString(),
                FlightNo = t["FlightNo"].ToString(),
                ImportDate = t["ImportDate"].ToString(),
                DeclDate = t["DeclDate"].ToString(),
                Currency = t["Currency"].ToString(),
                OrigPort = t["OrigPort"].ToString(),
                DeclType = t["DeclType"].ToString(),
                DeclNo = t["DeclNo"].ToString(),
                BagNo = t["BagNo"].ToString(),
                BagWeight = t["BagWeight"].ToString(),
                HawbNo = t["HawbNo"].ToString(),
                DeliveryType = t["DeliveryType"].ToString(),
                Ctns = t["Ctns"].ToString(),
                CtnUnit = t["CtnUnit"].ToString(),
                GrossWeight = t["GrossWeight"].ToString(),
                NetWeight = t["NetWeight"].ToString(),
                TermsSales = t["TermsSales"].ToString(),
                FreightAmt = t["FreightAmt"].ToString(),
                DutyExemption = t["DutyExemption"].ToString(),
                CTaxNo = t["CTaxNo"].ToString(),
                CName = t["CName"].ToString(),
                CAddr = t["CAddr"].ToString(),
                CTel = t["CTel"].ToString(),
                SName = t["SName"].ToString(),
                SAddr = t["SAddr"].ToString(),
                ItemNo = t["ItemNo"].ToString(),
                VendorItemId = t["VendorItemId"].ToString(),
                CategoryName = t["CategoryName"].ToString(),
                GoodsDesc = t["GoodsDesc"].ToString(),
                Uprice = t["Uprice"].ToString(),
                Qty = t["Qty"].ToString(),
                QtyUnit = t["QtyUnit"].ToString(),
                MfrCountry = t["MfrCountry"].ToString(),
                TaxMethod = t["TaxMethod"].ToString(),
                CCCCode = t["CCCCode"].ToString(),
                LicenseNo1 = t["LicenseNo1"].ToString(),
                LicenseNo2 = t["LicenseNo2"].ToString(),
                LicenseNo3 = t["LicenseNo3"].ToString(),
                Brand = t["Brand"].ToString(),
                Model = t["Model"].ToString(),
                Specification = t["Specification"].ToString(),
                DesignatedCode = t["DesignatedCode"].ToString(),
            }).ToList();

            // 預先處理 CoupangGoods 比對邏輯（只查詢一次）
            ProcessCoupangGoodsMatching(result);

            return result;
        }

        /// <summary>
        /// 處理 CoupangGoods 比對邏輯
        /// </summary>
        /// <param name="list"></param>
        private void ProcessCoupangGoodsMatching(List<ManifestModel> list)
        {
            // 取得 CoupangGoods 資料並轉換為字典以加快查詢速度
            var coupangGoodsDict = GetCoupangGoods()
                .Where(g => !string.IsNullOrEmpty(g.Goods))
                .GroupBy(g => g.Goods)
                .ToDictionary(g => g.Key, g => g.First());

            // 只處理 G1 類型且 OrigPort 為 HKHKG 的資料
            var gTypeHkItems = list.Where(r => r.DeclType == "G1" && r.OrigPort == "HKHKG" && !string.IsNullOrEmpty(r.GoodsDesc)).ToList();

            foreach (var item in gTypeHkItems)
            {
                // 使用字典進行快速比對
                var matchedGoods = coupangGoodsDict
                    .Where(kvp => item.GoodsDesc.Contains(kvp.Key))
                    .Select(kvp => kvp.Value)
                    .FirstOrDefault();

                if (matchedGoods != null)
                {
                    item.MatchedCountry = matchedGoods.Country;
                    item.MatchedProductName = matchedGoods.ProductName;
                }
                else
                {
                    // 比對不到，預設為 HK
                    item.MatchedCountry = "HK";
                    item.MatchedProductName = "";
                }
            }
        }

        public List<ManifestModel> GetManifestList(List<ManifestModel> list, string mawbNo)
        {
            return list.Where(r => r.MawbNo == mawbNo)
               .OrderBy(r => r.BagNo)
               .ThenByDescending(r => r.BagWeight)
               .ThenBy(r => r.HawbNo)
               .ThenBy(r => r.ItemNo)
               .ToList();
        }

        public List<ManifestModel> GetManifestXList(List<ManifestModel> list, string mawbNo)
        {
            return list.Where(r => (r.DeclType == "X2" || r.DeclType == "X3") && r.MawbNo == mawbNo)
                .OrderBy(r => r.BagNo)
                .ThenByDescending(r => r.BagWeight)
                .ThenBy(r => r.HawbNo)
                .ThenBy(r => r.ItemNo)
                .ToList();
        }

        public List<ManifestModel> GetManifestGList(List<ManifestModel> list, string mawbNo)
        {
            return list.Where(r => r.DeclType == "G1" && r.MawbNo == mawbNo)
               .OrderBy(r => r.BagNo)
               .ThenByDescending(r => r.BagWeight)
               .ThenBy(r => r.HawbNo)
               .ThenBy(r => r.ItemNo)
               .ToList();
        }

        public List<ManifestModel> GetAllManifestList(List<ManifestModel> list)
        {
            return list
                .OrderBy(r => r.DeclType)
                .ThenBy(r => r.MawbNo)
                .ThenBy(r => r.BagNo)
                .ThenByDescending(r => r.BagWeight)
                .ThenBy(r => r.HawbNo)
                .ThenBy(r => r.ItemNo)
                .ToList();
        }

        /// <summary>
        /// 空快清關明細表-Excel
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <param name="dataTime"></param>
        /// <returns></returns>
        public byte[] GetExcels(string sDate, string eDate, string dataTime)
        {
            //空快清關明細表
            var list = GetOrder_Manifest(sDate, eDate, dataTime);

            //主號
            var mawbNoList = list
                .GroupBy(r => r.MawbNo)
                .OrderBy(r => r.Key)
                .Select(r => r.Key)
                .ToList();

            using (MemoryStream zipStream = new MemoryStream())
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (var mawbNo in mawbNoList)
                    {
                        //取得主號Excel檔案
                        var result = GetMainNumberWorkbook(list, mawbNo);
                        var bytes = result.Item1;
                        var fileName = !string.IsNullOrEmpty(mawbNo) ? result.Item2 : "無主號";

                        ZipArchiveEntry excelEntry = archive.CreateEntry($"{fileName}.xlsx");
                        using (Stream entryStream = excelEntry.Open())
                        {
                            entryStream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }

                return zipStream.ToArray();
            }
        }

        public Tuple<byte[], string> GetMainNumberWorkbook(List<ManifestModel> list, string mawbNo)
        {
            IWorkbook workbook = new XSSFWorkbook();

            string sheetName;

            //X空快清關明細表
            var xList = GetManifestXList(list, mawbNo);
            sheetName = string.IsNullOrEmpty(mawbNo) ? $"捷豐清關明細-X類(無主號)" : $"捷豐清關明細-X類({mawbNo})";
            GetETLCCLDetailsSheet(workbook, xList, sheetName);

            //G空快清關明細表
            var gList = GetManifestGList(list, mawbNo);
            sheetName = string.IsNullOrEmpty(mawbNo) ? $"捷豐清關明細-G類(無主號)" : $"捷豐清關明細-G類({mawbNo})";
            GetETLCCLDetailsSheet(workbook, gList, sheetName);
            //G類明細
            sheetName = string.IsNullOrEmpty(mawbNo) ? $"G類資料-(無主號)" : $"G類資料-({mawbNo})";
            GetETLCCLDetailsGSheet(workbook, gList, sheetName);

            //[原]-頁籤
            var manifestList = GetManifestList(list, mawbNo);
            sheetName = string.IsNullOrEmpty(mawbNo) ? $"原(無主號)" : $"原({mawbNo})";
            GetETLCCLDetailsSheet(workbook, manifestList, sheetName);

            //袋號單
            sheetName = string.IsNullOrEmpty(mawbNo) ? $"袋號單(無主號)" : $"袋號單({mawbNo})";
            GetETLBagNumberSheet(workbook, manifestList, sheetName);

            //其他
            GetOtherSheet(workbook, manifestList);

            var item = xList.FirstOrDefault() ?? gList.FirstOrDefault();
            //日期
            DateTime.TryParseExact(
                    item.CreateDate,
                    "yyyy.MM.dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date);

            //總件數
            var total = manifestList
                .Select(r => r.BagNo)
                .Distinct()
                .Count();

            //派件
            var message = GetOtherMessage(manifestList);
            //日期+客戶(產地)+主號+航班號+倉儲+總件數+(派件)
            var fileName = $"{date.ToString("MMdd")}coupang({item?.MfrCountry}){mawbNo?.Insert(3,"-")} {item?.FlightNo}華儲{total}({message})";

            using (MemoryStream stream = new MemoryStream())
            {
                workbook.Write(stream);
                return new Tuple<byte[], string>(stream.ToArray(), fileName);
            }
        }

        /// <summary>
        /// 空快清關明細表-Excel-頁籤
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Details"></param>
        /// <param name="sheetName"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetETLCCLDetailsSheet(IWorkbook workbook, List<ManifestModel> dt_Details, string sheetName)
        {
            bool isTypeX = dt_Details.Any(r => r.DeclType.Contains("X2") || r.DeclType.Contains("X3"));

            ISheet sheet = workbook.CreateSheet(sheetName);
            
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy/m/d");
            
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("編號");
            row.CreateCell(1).SetCellValue("發貨公司");
            row.CreateCell(2).SetCellValue($"coupang({dt_Details.FirstOrDefault()?.MfrCountry})");
            row.CreateCell(4).SetCellValue("日期");
            NpoiCell.CreateDateTimeCell(row, 5, dt_Details.FirstOrDefault()?.CreateDate, dateStyle);
            row.CreateCell(7).SetCellValue("主單號");
            row.CreateCell(9).SetCellValue(dt_Details.FirstOrDefault()?.MawbNo.Insert(3, "-"));
            row.CreateCell(11).SetCellValue("納稅義務人代碼");
            row.CreateCell(12).SetCellValue("24951752");
            row.CreateCell(13).SetCellValue("航班號");
            row.CreateCell(14).SetCellValue(dt_Details.FirstOrDefault()?.FlightNo);
            row.CreateCell(15).SetCellValue("裝貨港代碼");
            row.CreateCell(17).SetCellValue("箱號");
            //表頭 
            row = sheet.CreateRow(1);
            row.CreateCell(1).SetCellValue("袋號");
            row.CreateCell(2).SetCellValue("袋重");
            row.CreateCell(3).SetCellValue("提單號碼");
            row.CreateCell(4).SetCellValue("件數");
            row.CreateCell(5).SetCellValue("提單重量");
            row.CreateCell(6).SetCellValue("品名");
            row.CreateCell(7).SetCellValue("數量");
            row.CreateCell(8).SetCellValue("單位");
            row.CreateCell(9).SetCellValue("產地");
            row.CreateCell(10).SetCellValue("單價");
            row.CreateCell(11).SetCellValue("寄件人公司");
            row.CreateCell(12).SetCellValue("寄件人");
            row.CreateCell(13).SetCellValue("收件人");
            row.CreateCell(14).SetCellValue("收件人公司電話");
            row.CreateCell(15).SetCellValue("收件人地址");
            row.CreateCell(16).SetCellValue("統編/身份證號碼");
            row.CreateCell(17).SetCellValue("派件公司");
            row.CreateCell(18).SetCellValue("CC款");
            row.CreateCell(19).SetCellValue("後段報關/一般倉");
            row.CreateCell(20).SetCellValue("發票金額");
            row.CreateCell(21).SetCellValue("備註");
            row.CreateCell(22).SetCellValue("MainHawbNo");
            row.CreateCell(23).SetCellValue(isTypeX ? "特許編號" : null);

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 3000);
            sheet.SetColumnWidth(2, 3000);
            sheet.SetColumnWidth(3, 4000);
            sheet.SetColumnWidth(4, 3000);
            sheet.SetColumnWidth(5, 3000);
            sheet.SetColumnWidth(6, 7000);
            sheet.SetColumnWidth(7, 3000);
            sheet.SetColumnWidth(8, 4000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);
            sheet.SetColumnWidth(11, 4000);
            sheet.SetColumnWidth(12, 4000);
            sheet.SetColumnWidth(13, 4000);
            sheet.SetColumnWidth(14, 5000);
            sheet.SetColumnWidth(15, 15000);
            sheet.SetColumnWidth(16, 5000);
            sheet.SetColumnWidth(17, 3000);
            sheet.SetColumnWidth(18, 3000);
            sheet.SetColumnWidth(19, 5000);
            sheet.SetColumnWidth(20, 3000);
            sheet.SetColumnWidth(21, 5000);
            sheet.SetColumnWidth(22, 5000);

            int irow = 2;
            int num;
            double dbl;
            string bagno = "";
            foreach (var item in dt_Details)
            {
                row = sheet.CreateRow(irow);

                //袋號 一樣的只顯示第一筆
                if (bagno != item.BagNo.ToString())
                {
                    bagno = item.BagNo.ToString();
                    row.CreateCell(1).SetCellValue(bagno);
                    //袋重
                    if (double.TryParse(item.BagWeight.ToString(), out dbl))
                    {
                        row.CreateCell(2).SetCellValue(dbl);
                    }
                    else
                    {
                        row.CreateCell(2).SetCellValue(item.BagWeight.ToString());
                    }
                }
                //}
                if (item.ItemNo == "1")
                {
                    //提單號碼
                    row.CreateCell(3).SetCellValue(item.HawbNo.ToString());
                    row.CreateCell(13).SetCellValue(item.CName.ToString());//收件人
                    row.CreateCell(14).SetCellValue(item.CTel.ToString());//收件人公司電話
                    row.CreateCell(15).SetCellValue(item.CAddr.ToString());//收件人地址
                    row.CreateCell(16).SetCellValue(item.CTaxNo.ToString());//ID
                    //提單重量
                    if (double.TryParse(item.GrossWeight.ToString(), out dbl))
                    {
                        row.CreateCell(5).SetCellValue(dbl);
                    }
                    else
                    {
                        row.CreateCell(5).SetCellValue(item.GrossWeight.ToString());
                    }
                }
                //else
                //{
                //row.CreateCell(3).SetCellValue("");
                //}

                //件數
                if (int.TryParse(item.Ctns.ToString(), out num))
                {
                    row.CreateCell(4).SetCellValue(num);
                }
                else
                {
                    row.CreateCell(4).SetCellValue(item.Ctns.ToString());
                }

                row.CreateCell(6).SetCellValue(item.GoodsDesc.ToString());//品名
                //數量
                if (int.TryParse(item.Qty.ToString(), out num))
                {
                    row.CreateCell(7).SetCellValue(num);
                }
                else
                {
                    row.CreateCell(7).SetCellValue(item.Qty.ToString());
                }
                row.CreateCell(8).SetCellValue(item.QtyUnit.ToString());//單位
                row.CreateCell(9).SetCellValue(item.MfrCountry.ToString());//產地
                //單價
                if (double.TryParse(item.Uprice.ToString(), out dbl))
                {
                    row.CreateCell(10).SetCellValue(dbl);
                }
                else
                {
                    row.CreateCell(10).SetCellValue(item.Uprice.ToString());
                }
                row.CreateCell(12).SetCellValue(item.SName.ToString());//寄件人
                row.CreateCell(17).SetCellValue(item.DeliveryType.ToString());
                row.CreateCell(21).SetCellValue(item.CategoryName.ToString());//備註
                row.CreateCell(22).SetCellValue(item.MainHawbNo.ToString());
                row.CreateCell(23).SetCellValue(item.DeclType == "X2" || item.DeclType == "X3" ? "A00070" : null);
                irow++;
            }
        }

        /// <summary>
        /// 袋號單-Excel-頁籤
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Details"></param>
        /// <param name="sheetName"></param>
        void GetETLBagNumberSheet(IWorkbook workbook, List<ManifestModel> dt_Details, string sheetName)
        {
            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow row = sheet.CreateRow(0);

            if (dt_Details.FirstOrDefault() != null && int.TryParse(dt_Details.FirstOrDefault().CreateDate.Replace(".", ""), out var createDate))
            {
                row.CreateCell(0).SetCellValue(createDate);
            }

            var flightNo = dt_Details.FirstOrDefault() != null ? dt_Details.FirstOrDefault().FlightNo : "";
            if (!string.IsNullOrEmpty(flightNo))
            {
                flightNo = flightNo.Replace(" ", "").Insert(2, " ");
                flightNo = flightNo.Substring(0, 3) + flightNo.Substring(3).PadLeft(4, '0');
            }

            row.CreateCell(1).SetCellValue(flightNo);
            row.CreateCell(2).SetCellValue(dt_Details.FirstOrDefault()?.MawbNo.Insert(3, "-"));
            row.CreateCell(3).SetCellValue("C2011");

            sheet.SetColumnWidth(0, 4000);
            sheet.SetColumnWidth(1, 4000);
            sheet.SetColumnWidth(2, 4000);
            sheet.SetColumnWidth(3, 4000);

            var bagNoCount = dt_Details
                            .GroupBy(r => new { r.BagNo })
                            .ToDictionary(g => g.Key.BagNo, g => g.Select(r => r.HawbNo).Distinct().Count());

            var irow = 1;
            //最後一個袋號
            var lastBagNo = "";
            dt_Details.ForEach(r =>
            {
                //一樣袋號只顯示一筆
                if (lastBagNo != r.BagNo)
                {
                    lastBagNo = r.BagNo;

                    var count = bagNoCount[r.BagNo];


                    var bagNo = count > 1 ? r.BagNo : r.HawbNo;

                    var mainHawbNo = bagNo.StartsWith("0H4") ||
                                     (bagNo == r.MainHawbNo && dt_Details.GroupBy(x => new { x.MainHawbNo, x.HawbNo }).Count(x => x.Key.MainHawbNo == r.MainHawbNo) == 1) ? ""
                                     : r.MainHawbNo;

                    row = sheet.CreateRow(irow);
                    row.CreateCell(0).SetCellValue(bagNo);
                    row.CreateCell(1).SetCellValue(r.Ctns);
                    row.CreateCell(2).SetCellValue(r.BagWeight);
                    row.CreateCell(3).SetCellValue(mainHawbNo);
                    irow++;
                }
            });

            //總筆數
            sheet.GetRow(0).CreateCell(4).SetCellValue(irow - 1);
        }

        /// <summary>
        /// 空快清關明細表-Excel-頁籤-G類資料
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Details"></param>
        /// <param name="sheetName"></param>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        void GetETLCCLDetailsGSheet(IWorkbook workbook, List<ManifestModel> dt_Details, string sheetName)
        {
            ISheet sheet = workbook.CreateSheet(sheetName);
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("DataCode");
            row.CreateCell(1).SetCellValue("SendId");
            row.CreateCell(2).SetCellValue("CreateDate");
            row.CreateCell(3).SetCellValue("MawbNo");
            row.CreateCell(4).SetCellValue("FlightNo");
            row.CreateCell(5).SetCellValue("ImportDate");
            row.CreateCell(6).SetCellValue("DeclDate");
            row.CreateCell(7).SetCellValue("Currency");
            row.CreateCell(8).SetCellValue("OrigPort");
            row.CreateCell(9).SetCellValue("DeclType");
            row.CreateCell(10).SetCellValue("DeclNo");
            row.CreateCell(11).SetCellValue("DutyExemption");
            row.CreateCell(12).SetCellValue("HawbNo");
            row.CreateCell(13).SetCellValue("Ctns");
            row.CreateCell(14).SetCellValue("CtnUnit");
            row.CreateCell(15).SetCellValue("GrossWeight");
            row.CreateCell(16).SetCellValue("NetWeight");
            row.CreateCell(17).SetCellValue("TermsSales");
            row.CreateCell(18).SetCellValue("FreightAmt");
            row.CreateCell(19).SetCellValue("TaxNo");
            row.CreateCell(20).SetCellValue("Name");
            row.CreateCell(21).SetCellValue("Addr");
            row.CreateCell(22).SetCellValue("Tel");
            row.CreateCell(23).SetCellValue("TaxNo");
            row.CreateCell(24).SetCellValue("Name");
            row.CreateCell(25).SetCellValue("Addr");
            row.CreateCell(26).SetCellValue("DeliveryType");
            row.CreateCell(27).SetCellValue("MasterBagWeight");
            row.CreateCell(28).SetCellValue("ItemNo");
            row.CreateCell(29).SetCellValue("GoodsDesc");
            row.CreateCell(30).SetCellValue("Uprice");
            row.CreateCell(31).SetCellValue("Qty");
            row.CreateCell(32).SetCellValue("QtyUnit");
            row.CreateCell(33).SetCellValue("MfrCountry");
            row.CreateCell(34).SetCellValue("TaxMethod");
            row.CreateCell(35).SetCellValue("CCCCode");
            row.CreateCell(36).SetCellValue("LicenseNo1");
            row.CreateCell(37).SetCellValue("LicenseNo2");
            row.CreateCell(38).SetCellValue("LicenseNo3");
            row.CreateCell(39).SetCellValue("Brand");
            row.CreateCell(40).SetCellValue("Model");
            row.CreateCell(41).SetCellValue("Specification");
            row.CreateCell(42).SetCellValue("DesignatedCode");
            row.CreateCell(43).SetCellValue("VendorItemId");
            row.CreateCell(44).SetCellValue("KAN4Category");
            row.CreateCell(45).SetCellValue("MainHawbNo");
            row.CreateCell(46).SetCellValue("對應產地");
            row.CreateCell(47).SetCellValue("對應中文品名");

            sheet.SetColumnWidth(0, 4000);
            sheet.SetColumnWidth(1, 4000);
            sheet.SetColumnWidth(2, 4000);
            sheet.SetColumnWidth(3, 4000);
            sheet.SetColumnWidth(4, 4000);
            sheet.SetColumnWidth(5, 4000);
            sheet.SetColumnWidth(6, 4000);
            sheet.SetColumnWidth(7, 4000);
            sheet.SetColumnWidth(8, 4000);
            sheet.SetColumnWidth(9, 4000);
            sheet.SetColumnWidth(10, 4000);
            sheet.SetColumnWidth(11, 4000);
            sheet.SetColumnWidth(12, 4000);
            sheet.SetColumnWidth(13, 4000);
            sheet.SetColumnWidth(14, 4000);
            sheet.SetColumnWidth(15, 4000);
            sheet.SetColumnWidth(16, 4000);
            sheet.SetColumnWidth(17, 4000);
            sheet.SetColumnWidth(18, 4000);
            sheet.SetColumnWidth(19, 4000);
            sheet.SetColumnWidth(20, 6000);
            sheet.SetColumnWidth(21, 15000);
            sheet.SetColumnWidth(22, 4000);
            sheet.SetColumnWidth(23, 4000);
            sheet.SetColumnWidth(24, 6000);
            sheet.SetColumnWidth(25, 10000);
            sheet.SetColumnWidth(26, 4000);
            sheet.SetColumnWidth(27, 4000);
            sheet.SetColumnWidth(28, 4000);
            sheet.SetColumnWidth(29, 4000);
            sheet.SetColumnWidth(30, 4000);
            sheet.SetColumnWidth(31, 4000);
            sheet.SetColumnWidth(32, 4000);
            sheet.SetColumnWidth(33, 4000);
            sheet.SetColumnWidth(34, 4000);
            sheet.SetColumnWidth(35, 4000);
            sheet.SetColumnWidth(36, 5000);
            sheet.SetColumnWidth(37, 5000);
            sheet.SetColumnWidth(38, 5000);
            sheet.SetColumnWidth(39, 4000);
            sheet.SetColumnWidth(40, 4000);
            sheet.SetColumnWidth(41, 4000);
            sheet.SetColumnWidth(42, 4000);
            sheet.SetColumnWidth(43, 4000);
            sheet.SetColumnWidth(44, 5000);
            sheet.SetColumnWidth(45, 5000);
            sheet.SetColumnWidth(46, 5000);
            sheet.SetColumnWidth(47, 5000);

            int irow = 1;
            foreach (var item in dt_Details)
            {
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue("G");
                row.CreateCell(1).SetCellValue(item.SendId);
                row.CreateCell(2).SetCellValue(item.CreateDate);
                row.CreateCell(3).SetCellValue(item.MawbNo);
                row.CreateCell(4).SetCellValue(item.FlightNo);
                row.CreateCell(5).SetCellValue(item.ImportDate);
                row.CreateCell(6).SetCellValue(item.DeclDate);
                row.CreateCell(7).SetCellValue(item.Currency);
                row.CreateCell(8).SetCellValue(item.OrigPort);
                row.CreateCell(9).SetCellValue(item.DeclType);
                row.CreateCell(10).SetCellValue(item.DeclNo);
                row.CreateCell(11).SetCellValue(item.DutyExemption);
                row.CreateCell(12).SetCellValue(item.HawbNo);
                row.CreateCell(13).SetCellValue(item.Ctns);
                row.CreateCell(14).SetCellValue(item.CtnUnit);
                row.CreateCell(15).SetCellValue(item.GrossWeight);
                row.CreateCell(16).SetCellValue(item.NetWeight);
                row.CreateCell(17).SetCellValue(item.TermsSales);
                row.CreateCell(18).SetCellValue(item.FreightAmt);
                row.CreateCell(19).SetCellValue(item.CTaxNo);
                row.CreateCell(20).SetCellValue(item.CName);
                row.CreateCell(21).SetCellValue(item.CAddr);
                row.CreateCell(22).SetCellValue(item.CTel);
                row.CreateCell(23).SetCellValue(item.CTaxNo);
                row.CreateCell(24).SetCellValue(item.SName);
                row.CreateCell(25).SetCellValue(item.SAddr);
                row.CreateCell(26).SetCellValue(item.DeliveryType);
                row.CreateCell(27).SetCellValue(item.BagWeight);
                row.CreateCell(28).SetCellValue(item.ItemNo);
                row.CreateCell(29).SetCellValue(item.GoodsDesc);
                row.CreateCell(30).SetCellValue(item.Uprice);
                row.CreateCell(31).SetCellValue(item.Qty);
                row.CreateCell(32).SetCellValue(item.QtyUnit);
                row.CreateCell(33).SetCellValue(item.MfrCountry);
                row.CreateCell(34).SetCellValue(item.TaxMethod);
                row.CreateCell(35).SetCellValue(item.CCCCode);
                row.CreateCell(36).SetCellValue(item.LicenseNo1);
                row.CreateCell(37).SetCellValue(item.LicenseNo2);
                row.CreateCell(38).SetCellValue(item.LicenseNo3);
                row.CreateCell(39).SetCellValue(item.Brand);
                row.CreateCell(40).SetCellValue(item.Model);
                row.CreateCell(41).SetCellValue(item.Specification);
                row.CreateCell(42).SetCellValue(item.DesignatedCode);
                row.CreateCell(43).SetCellValue(item.VendorItemId);
                row.CreateCell(44).SetCellValue(item.CategoryName);
                row.CreateCell(45).SetCellValue(item.MainHawbNo);
                
                // 直接使用預先處理好的比對結果
                row.CreateCell(46).SetCellValue(item.MatchedCountry ?? "");
                row.CreateCell(47).SetCellValue(item.MatchedProductName ?? "");
                irow++;
            }
        }

        /// <summary>
        /// 其他-Excel-頁籤
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Details"></param>
        /// <param name="sheetName"></param>
        void GetOtherSheet(IWorkbook workbook, List<ManifestModel> list)
        {
            var message = GetOtherMessage(list);
            var xMessage = GetOtherXMessage(list);
            var gMessage = GetOtherGMessage(list);

            ISheet sheet = workbook.CreateSheet("其他");
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue(message);

            row = sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue(xMessage);

            row = sheet.CreateRow(2);
            row.CreateCell(0).SetCellValue(gMessage);
        }

        /// <summary>
        /// 取得其他訊息
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        string GetOtherMessage(List<ManifestModel> list)
        {
            var deliveryTypes = list.GroupBy(r => r.DeliveryType)
                .Select(r => new
                {
                    r.Key,
                    Count = r.Select(x => x.BagNo).Distinct().Count()
                }).ToList();

            var message = $"派件 {string.Join("", deliveryTypes.Select(r => $"{r.Key}共{r.Count}件"))}";

            return message;

        }

        /// <summary>
        /// 取得其他X訊息
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        string GetOtherXMessage(List<ManifestModel> list)
        {
            var total = list
                     .Where(r => r.DeclType == "X2" || r.DeclType == "X3")
                     .Select(r => r.BagNo)
                     .Distinct()
                     .Count();

            var message = $"X類{total}件";

            return message;
        }

        /// <summary>
        /// 取得其他G訊息
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        string GetOtherGMessage(List<ManifestModel> list)
        {
            var total = list
             .Where(r => r.DeclType == "G1")
             .Select(r => r.BagNo)
             .Distinct()
             .Count();
            var message = $"G類{total}件";

            return message;
        }




        /// <summary>
        /// 新增空快清關明細表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResopnseModel InsertLog_ClearanceWork(LogClearanceWork model)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            resopnseModel.status = Status.success;

            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[Log_ClearanceWork]([WorkName],[DownloadTime],[UserId],[Ip]) ");
            sb.Append("values(@WorkName,@DownloadTime,@UserId,@Ip) ");
            using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
            {
                try
                {
                    conn.Open();
                    cmd.Parameters.Add("@WorkName", SqlDbType.NVarChar).Value = model.WorkName;
                    cmd.Parameters.Add("@DownloadTime", SqlDbType.NVarChar).Value = model.DownloadTime;
                    cmd.Parameters.Add("@UserId", SqlDbType.NVarChar).Value = model.UserId;
                    cmd.Parameters.Add("@Ip", SqlDbType.NVarChar).Value = model.Ip;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                }
                finally
                {
                    conn.Close();
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 取得 Coupang 商品資料
        /// </summary>
        /// <returns></returns>
        private List<CoupangGoodsModel> GetCoupangGoods()
        {
            var sql = @"SELECT [Goods], [Country], [ProductName] 
                       FROM [jetf].[dbo].[CoupangGoods]";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.Fill(dt);
            }

            var result = dt.AsEnumerable().Select(r => new CoupangGoodsModel
            {
                Goods = r["Goods"].ToString(),
                Country = r["Country"].ToString(),
                ProductName = r["ProductName"].ToString()
            }).ToList();

            return result;
        }

    }
}
