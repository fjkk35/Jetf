using Dapper;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.ShipmentInboundProcess.Domain;
using Service.Services.ShipmentInboundRecord.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Service.Services.ShipmentInboundRecord
{
    public class ShipmentInboundRecordService : _BaseService
    {
        /// <summary>
        /// 根據 Id 取得貨件詳細資料
        /// </summary>
        /// <param name="id">ShipmentInbound 的 Id</param>
        /// <returns>貨件詳細資料</returns>
        public ShipmentInboundRecordModel GetDetailById(int id)
        {
            if (id <= 0)
            {
                return null;
            }

            var sql = @"
                SELECT [Id]
                      ,[DataType]
                      ,[InboundDate]
                      ,[CustCode]
                      ,[TransNo]
                      ,[TrackingNo]
                      ,[SourceType]
                      ,[SeqNo]
                      ,[LocationCode]
                      ,[ProcessType]
                      ,[ReturnReason]
                      ,[ReturnTrackingNo]
                      ,[FreightPayerNo]
                      ,[Tax]
                      ,[Fee]
                      ,[Ccfee]
                      ,[Cod]
                      ,[FreightFee]
                      ,[ProcessFee]
                      ,[ProcessTransNo]
                      ,[ProcessTime]
                      ,[ProcessOpe]
                      ,[Remark]
                      ,[ProcessImporter]
                      ,[ProcessImporterPhone]
                      ,[ProcessImporterAddr]
                      ,[StoreCode]
                      ,[StoreName]
                      ,[CarNo]
                      ,[PickupTime]
                      ,[OutboundDate]
                      ,[OutboundTime]
                      ,[OutboundOpe]
                      ,[OutboundTrackingNo]
                      ,[WarehouseProcessType]
                      ,[WarehouseProcessTime]
                      ,[WarehouseProcessOpe]
                FROM [jetf].[dbo].[ShipmentInbound]
                WHERE [Id] = @Id";

            var data = conn.QueryFirstOrDefault<ShipmentInboundRecordModel>(sql, new { Id = id });

            if (data != null)
            {
                FillCustomerAndTransNames(new List<ShipmentInboundRecordModel> { data });
            }

            return data;
        }

        public ShipmentInboundRecordResponse GetData(ShipmentInboundRecordRequest request)
        {
            var parameters = new DynamicParameters();

            var countSql = new StringBuilder();
            countSql.AppendLine("SELECT COUNT(1) FROM [jetf].[dbo].[ShipmentInbound]");
            BuildWhereConditions(countSql, request, parameters);

            var sql = new StringBuilder();
            sql.AppendLine("SELECT [Id]");
            sql.AppendLine("      ,[DataType]");
            sql.AppendLine("      ,[InboundDate]");
            sql.AppendLine("      ,[CustCode]");
            sql.AppendLine("      ,[TransNo]");
            sql.AppendLine("      ,[TrackingNo]");
            sql.AppendLine("      ,[SourceType]");
            sql.AppendLine("      ,[SeqNo]");
            sql.AppendLine("      ,[LocationCode]");
            sql.AppendLine("      ,[ProcessType]");
            sql.AppendLine("      ,[ReturnTrackingNo]");
            sql.AppendLine("      ,[FreightPayerNo]");
            sql.AppendLine("      ,[Tax]");
            sql.AppendLine("      ,[Fee]");
            sql.AppendLine("      ,[Ccfee]");
            sql.AppendLine("      ,[Cod]");
            sql.AppendLine("      ,[FreightFee]");
            sql.AppendLine("      ,[ProcessTransNo]");
            sql.AppendLine("      ,[ProcessTime]");
            sql.AppendLine("      ,[ProcessOpe]");
            sql.AppendLine("      ,[Remark]");
            sql.AppendLine("      ,[ProcessImporter]");
            sql.AppendLine("      ,[ProcessImporterPhone]");
            sql.AppendLine("      ,[ProcessImporterAddr]");
            sql.AppendLine("      ,[StoreCode]");
            sql.AppendLine("      ,[StoreName]");
            sql.AppendLine("      ,[CarNo]");
            sql.AppendLine("      ,[PickupTime]");
            sql.AppendLine("      ,[OutboundDate]");
            sql.AppendLine("      ,[OutboundTime]");
            sql.AppendLine("      ,[OutboundOpe]");
            sql.AppendLine("      ,[OutboundTrackingNo]");
            sql.AppendLine("      ,[WarehouseProcessType]");
            sql.AppendLine("      ,[WarehouseProcessTime]");
            sql.AppendLine("      ,[WarehouseProcessOpe]");
            sql.AppendLine("FROM [jetf].[dbo].[ShipmentInbound]");
            BuildWhereConditions(sql, request, parameters);

            sql.AppendLine("ORDER BY [InboundDate] DESC");
            sql.AppendLine($"OFFSET {(request.Page - 1) * request.PageSize} ROWS");
            sql.AppendLine($"FETCH NEXT {request.PageSize} ROWS ONLY");

            var totalCount = conn.QueryFirstOrDefault<int>(countSql.ToString(), parameters);
            var data = conn.Query<ShipmentInboundRecordModel>(sql.ToString(), parameters).ToList();

            FillCustomerAndTransNames(data);

            return new ShipmentInboundRecordResponse
            {
                Data = data,
                TotalCount = totalCount
            };
        }

        private void BuildWhereConditions(StringBuilder sql, ShipmentInboundRecordRequest request, DynamicParameters parameters)
        {
            sql.AppendLine("WHERE 1=1");


            sql.WhereIf(DateTime.TryParse(request.InboundDateStart, out var startDate), "[InboundDate] >= @InboundDateStart", parameters, p =>
            {
                p.Add("InboundDateStart", startDate);
            });

            sql.WhereIf(DateTime.TryParse(request.InboundDateEnd, out var endDate), "[InboundDate] < @InboundDateEnd", parameters, p =>
            {
                p.Add("InboundDateEnd", endDate.AddDays(+1));
            });

            sql.WhereIf(!string.IsNullOrWhiteSpace(request.DataType), "[DataType] = @DataType", parameters, p =>
            {
                p.Add("DataType", request.DataType);
            });

            sql.WhereIf(!string.IsNullOrWhiteSpace(request.ProcessType), "[ProcessType] = @ProcessType", parameters, p =>
            {
                p.Add("ProcessType", request.ProcessType);
            });

            sql.WhereIf(!string.IsNullOrWhiteSpace(request.LocationCode), "[LocationCode] LIKE @LocationCode", parameters, p =>
            {
                p.Add("LocationCode", $"%{request.LocationCode}%");
            });

            sql.WhereIf(!string.IsNullOrWhiteSpace(request.WarehouseProcessType), "[WarehouseProcessType] = @WarehouseProcessType", parameters, p =>
            {
                p.Add("WarehouseProcessType", request.WarehouseProcessType);
            });

            // 客戶
            sql.WhereIf(request.CustCodes?.Any() == true, "[CustCode] IN @CustCodes", parameters, p =>
            {
                p.Add("CustCodes", request.CustCodes);
            });

            sql.WhereIf(!string.IsNullOrWhiteSpace(request.SourceType), "[SourceType] = @SourceType", parameters, p =>
            {
                p.Add("SourceType", request.SourceType);
            });

            sql.WhereIf(!string.IsNullOrWhiteSpace(request.TrackingNo), "[TrackingNo] LIKE @TrackingNo", parameters, p =>
            {
                p.Add("TrackingNo", $"%{request.TrackingNo}%");
            });
        }

        private Dictionary<string, string> GetAirCustNames(List<string> custCodes)
        {
            if (!custCodes.Any())
                return new Dictionary<string, string>();

            var sql = "SELECT distinct OLD_CODE as Cust_Code, Cust_Name FROM DATA_CENTER.dbo.SYS_CUST WHERE CUST_TYPE='AIR' AND OLD_CODE > ''";
            var custs = conn.Query<dynamic>(sql).ToList();
            return custs.ToDictionary(
                x => (string)x.Cust_Code,
                x => (string)x.Cust_Name
            );
        }

        private Dictionary<string, string> GetSeaCustNames(List<string> custCodes)
        {
            if (!custCodes.Any())
                return new Dictionary<string, string>();

            var sql = "SELECT distinct Cust_Code, Cust_Name FROM DATA_CENTER.dbo.SYS_CUST WHERE CUST_TYPE='SEA'";
            var custs = conn.Query<dynamic>(sql).ToList();
            return custs.ToDictionary(
                x => (string)x.Cust_Code,
                x => (string)x.Cust_Name
            );
        }

        private Dictionary<string, string> GetAirTransNames(List<string> transNos)
        {
            if (!transNos.Any())
                return new Dictionary<string, string>();

            var sql = "SELECT distinct TRANS_NO, TRANS_NAME FROM [jetf].[dbo].customer_master WHERE TRAN_TYPE='空運'";
            var transList = conn.Query<dynamic>(sql).ToList();
            return transList
                .GroupBy(x => (string)x.TRANS_NO)
                .ToDictionary(
                    g => g.Key,
                    g => (string)g.First().TRANS_NAME
                );
        }

        private void FillCustomerAndTransNames(List<ShipmentInboundRecordModel> data)
        {
            var airCustCodes = data.Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.CustCode))
                                   .Select(x => x.CustCode)
                                   .Distinct()
                                   .ToList();

            var seaCustCodes = data.Where(x => x.DataType == "海運" && !string.IsNullOrWhiteSpace(x.CustCode))
                                   .Select(x => x.CustCode)
                                   .Distinct()
                                   .ToList();

            var airTransNos = data.Where(x => x.DataType == "空運" && !string.IsNullOrWhiteSpace(x.TransNo))
                                  .Select(x => x.TransNo)
                                  .Distinct()
                                  .ToList();

            var airCustNames = GetAirCustNames(airCustCodes);
            var seaCustNames = GetSeaCustNames(seaCustCodes);
            var airTransNames = GetAirTransNames(airTransNos);

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

                if (item.DataType == "空運" && !string.IsNullOrWhiteSpace(item.TransNo) && airTransNames.ContainsKey(item.TransNo))
                {
                    item.TransName = airTransNames[item.TransNo];
                }
            }
        }

        /// <summary>
        /// 取得儲位歷史紀錄
        /// </summary>
        /// <param name="shipmentInboundId">ShipmentInbound 的 Id</param>
        /// <returns>儲位歷史紀錄列表</returns>
        public List<ShipmentInboundLocationHistoryModel> GetLocationHistory(int shipmentInboundId)
        {
            if (shipmentInboundId <= 0)
            {
                return new List<ShipmentInboundLocationHistoryModel>();
            }

            var sql = @"
                SELECT 
                    Id,
                    ShipmentInboundId,
                    OldLocationCode,
                    NewLocationCode,
                    CreatedOpe,
                    CreatedTime
                FROM [jetf].[dbo].[ShipmentInboundLocationHistory]
                WHERE ShipmentInboundId = @ShipmentInboundId
                ORDER BY CreatedTime DESC";

            var result = conn.Query<ShipmentInboundLocationHistoryModel>(sql, new { ShipmentInboundId = shipmentInboundId }).ToList();

            return result;
        }

        /// <summary>
        /// 取得客戶清單
        /// </summary>
        public Dictionary<string,List<SelectListModel>> GetCustList()
        {
            var sql = @"
                SELECT * FROM (
                    SELECT DISTINCT Cust_Type,Cust_Code, Cust_Name FROM DATA_CENTER.dbo.SYS_CUST
                    WHERE CUST_TYPE='SEA' 
                    UNION ALL
                    SELECT DISTINCT Cust_Type,OLD_CODE AS Cust_Code, Cust_Name FROM DATA_CENTER.dbo.SYS_CUST
                    WHERE CUST_TYPE='AIR' AND OLD_CODE > ''
                ) a
                ORDER BY Cust_Code";

            var data = conn.Query<ShipmentInboundCustomerModel>(sql).ToList();

            return data.GroupBy(r => r.TypeName)
                            .ToDictionary(g => g.Key, g => g.Select(x => new SelectListModel
                            {
                                 Value = x.Cust_Code,
                                 Text = x.Cust_Name
                            })
                            .ToList());
        }

        /// <summary>
        /// 取得貨物來源清單
        /// </summary>
        public List<SelectListModel> GetSourceTypeList()
        {
            return Enum.GetValues(typeof(ShipmentInboundSourceType))
                .Cast<ShipmentInboundSourceType>()
                .Select(item => new SelectListModel
                {
                    Value = ((int)item).ToString(),
                    Text = item.ToDescription()
                }).ToList();
        }

        public ShipmentInboundRecordExportExcelResult GetExportExcel(ShipmentInboundRecordRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // 匯出不分頁：保留原查詢條件，但以大筆數避免被分頁限制
            var exportRequest = new ShipmentInboundRecordRequest
            {
                InboundDateStart = request.InboundDateStart,
                InboundDateEnd = request.InboundDateEnd,
                ProcessType = request.ProcessType,
                LocationCode = request.LocationCode,
                CustCode = request.CustCode,
                CustCodes = request.CustCodes,
                SourceType = request.SourceType,
                TrackingNo = request.TrackingNo,
                DataType = request.DataType,
                WarehouseProcessType = request.WarehouseProcessType,
                Page = 1,
                PageSize = 100000
            };

            var dataResult = GetData(exportRequest);
            var data = dataResult?.Data ?? new List<ShipmentInboundRecordModel>();

            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("報表");

            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);
            var numberStyle = NpoiStyle.CreateNumberStyle(workbook);
            var dateStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd");
            var dateTimeStyle = NpoiStyle.CreateDateTimeStyle(workbook, "yyyy-mm-dd hh:mm:ss");

            var headers = new List<string>
            {
                "序號",
                "入庫日期",
                "進口方式",
                "客戶",
                "單號",
                "貨件來源",
                "處理方式",
                "客服處理日期",
                "客服處理人",
                "出庫日期",
                "出庫操作日",
                "出庫操作人",
                "倉庫狀態",
                "倉庫狀態操作日",
                "倉庫狀態操作人",
                "重出派件公司",
                "收件人",
                "電話",
                "宅配地址",
                "門市店號",
                "門市名稱",
                "運費支付方",
                "代收款總金額",
                "備註",
                "流水號",
                "儲位",
                "重出日期",
                "重出單號",
                "到付款",
                "運費",
                "稅金",
                "報關費",
                "代收手續費"
            };

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateHeaderCells(headerRow, headers, headerStyle);

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var row = sheet.CreateRow(i + 1);

                int c = 0;
                NpoiCell.CreateIntCell(row, c++, i + 1, numberStyle);

                NpoiCell.CreateDateTimeCell(row, c++, item.InboundDate, dateStyle);
                NpoiCell.CreateCell(row, c++, item.DataType, dataStyle);
                NpoiCell.CreateCell(row, c++, string.IsNullOrWhiteSpace(item.CustName) ? item.CustCode : item.CustName, dataStyle);
                NpoiCell.CreateCell(row, c++, item.TrackingNo, dataStyle);
                NpoiCell.CreateCell(row, c++, item.SourceTypeName, dataStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessTypeName, dataStyle);

                NpoiCell.CreateDateTimeCell(row, c++, item.ProcessTime, dateTimeStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessOpe, dataStyle);

                NpoiCell.CreateDateTimeCell(row, c++, item.OutboundDate, dateStyle);
                NpoiCell.CreateDateTimeCell(row, c++, item.OutboundTime, dateTimeStyle);
                NpoiCell.CreateCell(row, c++, item.OutboundOpe, dataStyle);

                NpoiCell.CreateCell(row, c++, item.WarehouseProcessName, dataStyle);
                NpoiCell.CreateDateTimeCell(row, c++, item.WarehouseProcessTime, dateTimeStyle);
                NpoiCell.CreateCell(row, c++, item.WarehouseProcessOpe, dataStyle);

                NpoiCell.CreateCell(row, c++, item.ProcessTransName, dataStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessImporter, dataStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessImporterPhone, dataStyle);
                NpoiCell.CreateCell(row, c++, item.ProcessImporterAddr, dataStyle);
                NpoiCell.CreateCell(row, c++, item.StoreCode, dataStyle);
                NpoiCell.CreateCell(row, c++, item.StoreName, dataStyle);
                NpoiCell.CreateCell(row, c++, item.FreightPayerName, dataStyle);

                NpoiCell.CreateIntCell(row, c++, item.TotalAmount, numberStyle);
                NpoiCell.CreateCell(row, c++, item.Remark, dataStyle);
                NpoiCell.CreateCell(row, c++, item.SeqNo, dataStyle);
                NpoiCell.CreateCell(row, c++, item.LocationCode, dataStyle);

                NpoiCell.CreateDateTimeCell(row, c++, item.OutboundDate, dateStyle);
                NpoiCell.CreateCell(row, c++, item.OutboundTrackingNo, dataStyle);

                NpoiCell.CreateIntCell(row, c++, item.Cod, numberStyle);
                NpoiCell.CreateIntCell(row, c++, item.FreightFee, numberStyle);
                NpoiCell.CreateIntCell(row, c++, item.Tax, numberStyle);
                NpoiCell.CreateIntCell(row, c++, item.Ccfee, numberStyle);
                NpoiCell.CreateIntCell(row, c++, item.Fee, numberStyle);
            }

            sheet.AutoSizeColumns(headers.Count, scale: 1.15, minWidth: 10);

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.Write(ms);
                bytes = ms.ToArray();
            }

            var fileName = $"貨件紀錄查詢_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return new ShipmentInboundRecordExportExcelResult
            {
                FileName = fileName,
                FileBytes = bytes
            };
        }

        /// <summary>
        /// 更新金額欄位
        /// </summary>
        public void UpdateAmount(UpdateAmountRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Id <= 0)
            {
                throw new ArgumentException("Id 不可為空");
            }

            if (string.IsNullOrWhiteSpace(request.FieldName))
            {
                throw new ArgumentException("FieldName 不可為空");
            }

            var allowedFields = new[] { "Cod", "Tax", "Ccfee" };
            if (!allowedFields.Contains(request.FieldName))
            {
                throw new ArgumentException("不允許修改此欄位");
            }

            var querySql = $@"
                SELECT [{request.FieldName}]
                FROM [jetf].[dbo].[ShipmentInbound]
                WHERE [Id] = @Id";

            var oldValue = conn.QueryFirstOrDefault<int?>(querySql, new { Id = request.Id });

            if (oldValue == request.NewValue)
            {
                return;
            }

            var updateSql = $@"
                UPDATE [jetf].[dbo].[ShipmentInbound]
                SET [{request.FieldName}] = @NewValue
                WHERE [Id] = @Id";

            conn.Execute(updateSql, new { Id = request.Id, NewValue = request.NewValue });

            var insertHistorySql = @"
                INSERT INTO [jetf].[dbo].[ShipmentInboundEditHistory]
                ([ShipmentInboundId], [FieldName], [OldValue], [NewValue], [EditTime], [EditUser])
                VALUES
                (@ShipmentInboundId, @FieldName, @OldValue, @NewValue, @EditTime, @EditUser)";

            conn.Execute(insertHistorySql, new
            {
                ShipmentInboundId = request.Id,
                FieldName = request.FieldName,
                OldValue = oldValue?.ToString(),
                NewValue = request.NewValue.ToString(),
                EditTime = DateTime.Now,
                EditUser = GetUserId()
            });
        }

        /// <summary>
        /// 取得編輯歷史記錄
        /// </summary>
        public List<ShipmentInboundEditHistoryModel> GetEditHistory(int shipmentInboundId)
        {
            if (shipmentInboundId <= 0)
            {
                return new List<ShipmentInboundEditHistoryModel>();
            }

            var sql = @"
                SELECT 
                    [Id],
                    [ShipmentInboundId],
                    [FieldName],
                    [OldValue],
                    [NewValue],
                    [EditTime],
                    [EditUser]
                FROM [jetf].[dbo].[ShipmentInboundEditHistory]
                WHERE [ShipmentInboundId] = @ShipmentInboundId
                ORDER BY [EditTime] DESC";

            var result = conn.Query<ShipmentInboundEditHistoryModel>(sql, new { ShipmentInboundId = shipmentInboundId }).ToList();

            return result;
        }
    }
}
