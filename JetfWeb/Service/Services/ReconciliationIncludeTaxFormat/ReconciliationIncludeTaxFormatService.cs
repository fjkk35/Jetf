using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.ReconciliationIncludeTaxFormat.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data;
using System.Linq;

namespace Service.Services.ReconciliationIncludeTaxFormat
{
    /// <summary>
    /// 包稅客戶 Excel 匯出格式管理服務。
    /// </summary>
    public sealed class ReconciliationIncludeTaxFormatService : _BaseService
    {
        private static readonly IReadOnlyList<ReconciliationIncludeTaxFieldOption> FieldOptions =
            new List<ReconciliationIncludeTaxFieldOption>
            {
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMaster.OutDateTime",
                    Name = "出倉時間",
                    DataPath = "FEE_MASTER.OUT_DATETIME"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMaster.Type",
                    Name = "報關類別",
                    DataPath = "FEE_MASTER.TYPE"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMaster.Customer",
                    Name = "客戶",
                    DataPath = "FEE_MASTER.CUSTOMER"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMasterDetail.TaxNumber",
                    Name = "稅單號碼",
                    DataPath = "FEE_MASTER_DETAIL.TAX_NUMBER"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMasterDetail.MainNumber",
                    Name = "主號",
                    DataPath = "FEE_MASTER_DETAIL.MAIN_NUMBER"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMasterDetail.BagNumber",
                    Name = "清關袋號",
                    DataPath = "FEE_MASTER_DETAIL.BAG_NUMBER"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMasterDetail.TrackingNo",
                    Name = "分提單號",
                    DataPath = "FEE_MASTER_DETAIL.TRACKINGNO"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMasterDetail.DlvInv",
                    Name = "物流貨號",
                    DataPath = "FEE_MASTER_DETAIL.DLV_INV"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMasterDetail.TaxPayer",
                    Name = "納稅義務人",
                    DataPath = "FEE_MASTER_DETAIL.TAX_PAYER"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMasterDetail.Tax",
                    Name = "稅金",
                    DataPath = "FEE_MASTER_DETAIL.TAX"
                },
                new ReconciliationIncludeTaxFieldOption
                {
                    Key = "FeeMasterDetail.TaxBase",
                    Name = "稅基",
                    DataPath = "FEE_MASTER_DETAIL.TAX_BASE"
                }
            };

        /// <summary>
        /// 建立包稅客戶格式管理服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">資料中心資料庫內容。</param>
        public ReconciliationIncludeTaxFormatService(
            JetfDbContext jetfDbContext,
            DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 查詢所有包稅客戶匯出格式。
        /// </summary>
        /// <returns>格式清單。</returns>
        public List<ReconciliationIncludeTaxFormatListItem> Search()
        {
            return JetfDb.ReconciliationIncludeTaxFormats
                .AsNoTracking()
                .OrderBy(x => x.FormatName)
                .Select(x => new ReconciliationIncludeTaxFormatListItem
                {
                    Id = x.Id,
                    FormatName = x.FormatName,
                    ColumnCount = x.Columns.Count()
                })
                .ToList();
        }

        /// <summary>
        /// 取得可供格式使用的資料庫欄位。
        /// </summary>
        /// <returns>欄位選項。</returns>
        public List<ReconciliationIncludeTaxFieldOption> GetFieldOptions()
        {
            return FieldOptions
                .Select(x => new ReconciliationIncludeTaxFieldOption
                {
                    Key = x.Key,
                    Name = x.Name,
                    DataPath = x.DataPath
                })
                .ToList();
        }

        /// <summary>
        /// 依已儲存的欄位設定建立包稅客戶 Excel 明細工作表。
        /// </summary>
        /// <param name="data">由 FEE_MASTER／FEE_MASTER_DETAIL 查詢出的資料。</param>
        /// <param name="formatId">匯出格式識別碼。</param>
        /// <returns>完成欄位排序與固定值套用的 Excel 活頁簿。</returns>
        public IWorkbook CreateExcelWorkbook(DataTable data, int formatId)
        {
            var format = GetDetail(formatId);
            var workbook = new XSSFWorkbook();
            // 格式名稱可達 50 字且可能包含 Excel 禁用字元，頁籤固定使用安全名稱。
            var sheet = workbook.CreateSheet("包稅明細");
            var headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
            var dataStyle = NpoiStyle.CreateDataStyle(workbook);

            NpoiCell.CreateHeaderCells(
                sheet.CreateRow(0),
                format.Columns.Select(x => x.ColumnName).ToList(),
                headerStyle);

            var rows = data ?? new DataTable();
            for (var rowIndex = 0; rowIndex < rows.Rows.Count; rowIndex++)
            {
                var excelRow = sheet.CreateRow(rowIndex + 1);
                var dataRow = rows.Rows[rowIndex];
                for (var columnIndex = 0; columnIndex < format.Columns.Count; columnIndex++)
                {
                    var column = format.Columns[columnIndex];
                    var value = column.SourceType == ReconciliationIncludeTaxColumnSourceType.Constant
                        ? column.DefaultValue
                        : GetDataTableValue(dataRow, column.FieldKey);
                    NpoiCell.CreateCell(excelRow, columnIndex, value, dataStyle);
                }
            }

            sheet.AutoSizeColumns(format.Columns.Count, minWidth: 12);
            return workbook;
        }

        /// <summary>
        /// 取得單一格式及其欄位設定。
        /// </summary>
        /// <param name="id">格式識別碼。</param>
        /// <returns>格式明細。</returns>
        public ReconciliationIncludeTaxFormatDetail GetDetail(int id)
        {
            var entity = JetfDb.ReconciliationIncludeTaxFormats
                .AsNoTracking()
                .Include(x => x.Columns)
                .FirstOrDefault(x => x.Id == id);

            if (entity == null)
            {
                throw new ArgumentException("找不到包稅客戶匯出格式。");
            }

            return new ReconciliationIncludeTaxFormatDetail
            {
                Id = entity.Id,
                FormatName = entity.FormatName,
                Columns = entity.Columns
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Id)
                    .Select(ToColumnRequest)
                    .ToList()
            };
        }

        /// <summary>
        /// 儲存包稅客戶匯出格式及其欄位設定。
        /// </summary>
        /// <param name="request">格式儲存請求。</param>
        public void Save(ReconciliationIncludeTaxFormatSaveRequest request)
        {
            ValidateRequest(request);

            var formatName = request.FormatName.Trim();
            var duplicate = JetfDb.ReconciliationIncludeTaxFormats
                .Any(x => x.FormatName == formatName &&
                         (!request.Id.HasValue || x.Id != request.Id.Value));
            if (duplicate)
            {
                throw new ArgumentException("格式名稱不可重複。");
            }

            using (var transaction = JetfDb.Database.BeginTransaction())
            {
                try
                {
                    ReconciliationIncludeTaxFormatEntity entity;
                    if (request.Id.HasValue)
                    {
                        entity = JetfDb.ReconciliationIncludeTaxFormats
                            .FirstOrDefault(x => x.Id == request.Id.Value);
                        if (entity == null)
                        {
                            throw new ArgumentException("找不到包稅客戶匯出格式。");
                        }

                        entity.FormatName = formatName;
                        entity.UpdatedDate = DateTime.Now;
                        JetfDb.ReconciliationIncludeTaxFormatColumns.RemoveRange(
                            JetfDb.ReconciliationIncludeTaxFormatColumns
                                .Where(x => x.FormatId == entity.Id));
                    }
                    else
                    {
                        entity = new ReconciliationIncludeTaxFormatEntity
                        {
                            FormatName = formatName,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        };
                        JetfDb.ReconciliationIncludeTaxFormats.Add(entity);
                        JetfDb.SaveChanges();
                    }

                    var columns = request.Columns
                        .Select((column, index) => new ReconciliationIncludeTaxFormatColumnEntity
                        {
                            FormatId = entity.Id,
                            SortOrder = index,
                            ColumnName = column.ColumnName.Trim(),
                            SourceType = column.SourceType,
                            FieldKey = column.SourceType == ReconciliationIncludeTaxColumnSourceType.Field
                                ? column.FieldKey.Trim()
                                : null,
                            DefaultValue = column.SourceType == ReconciliationIncludeTaxColumnSourceType.Constant
                                ? (column.DefaultValue ?? string.Empty).Trim()
                                : null
                        })
                        .ToList();

                    JetfDb.ReconciliationIncludeTaxFormatColumns.AddRange(columns);
                    JetfDb.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// 刪除包稅客戶匯出格式。
        /// </summary>
        /// <param name="id">格式識別碼。</param>
        public void Delete(int id)
        {
            var entity = JetfDb.ReconciliationIncludeTaxFormats.FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                throw new ArgumentException("找不到包稅客戶匯出格式。");
            }

            JetfDb.ReconciliationIncludeTaxFormatColumns.RemoveRange(
                JetfDb.ReconciliationIncludeTaxFormatColumns.Where(x => x.FormatId == id));
            JetfDb.ReconciliationIncludeTaxFormats.Remove(entity);
            JetfDb.SaveChanges();
        }

        /// <summary>
        /// 將欄位實體轉為畫面儲存模型。
        /// </summary>
        /// <param name="entity">欄位實體。</param>
        /// <returns>欄位請求模型。</returns>
        private static ReconciliationIncludeTaxFormatColumnRequest ToColumnRequest(
            ReconciliationIncludeTaxFormatColumnEntity entity)
        {
            return new ReconciliationIncludeTaxFormatColumnRequest
            {
                Id = entity.Id,
                ColumnName = entity.ColumnName,
                SourceType = entity.SourceType,
                FieldKey = entity.FieldKey,
                DefaultValue = entity.DefaultValue
            };
        }

        /// <summary>
        /// 依格式欄位代碼從查詢結果讀取匯出值。
        /// </summary>
        /// <param name="row">資料列。</param>
        /// <param name="fieldKey">格式欄位代碼。</param>
        /// <returns>匯出文字。</returns>
        private static string GetDataTableValue(DataRow row, string fieldKey)
        {
            if (row == null || string.IsNullOrWhiteSpace(fieldKey))
            {
                return string.Empty;
            }

            switch (fieldKey)
            {
                case "FeeMaster.OutDateTime":
                    return GetDataTableText(row, "OUT_DATETIME");
                case "FeeMaster.Type":
                    return GetDataTableText(row, "TYPE");
                case "FeeMaster.Customer":
                    return GetDataTableText(row, "CUSTOMER", "CUST_ID");
                case "FeeMasterDetail.TaxNumber":
                    return GetDataTableText(row, "TAX_NUMBER");
                case "FeeMasterDetail.MainNumber":
                    return GetDataTableText(row, "MAIN_NUMBER");
                case "FeeMasterDetail.BagNumber":
                    return GetDataTableText(row, "BAG_NUMBER");
                case "FeeMasterDetail.TrackingNo":
                    return GetDataTableText(row, "TRACKINGNO");
                case "FeeMasterDetail.DlvInv":
                    return GetDataTableText(row, "DLV_INV");
                case "FeeMasterDetail.TaxPayer":
                    return GetDataTableText(row, "TAX_PAYER");
                case "FeeMasterDetail.Tax":
                    if (HasColumn(row, "TAX"))
                    {
                        return GetDataTableText(row, "TAX");
                    }

                    return (GetDataTableInt64(row, "TAX1") + GetDataTableInt64(row, "TAX2"))
                        .ToString();
                case "FeeMasterDetail.TaxBase":
                    return GetDataTableText(row, "TAX_BASE");
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 讀取資料列中的第一個存在欄位。
        /// </summary>
        /// <param name="row">資料列。</param>
        /// <param name="columnNames">候選欄位名稱。</param>
        /// <returns>欄位文字。</returns>
        private static string GetDataTableText(DataRow row, params string[] columnNames)
        {
            foreach (var columnName in columnNames)
            {
                if (HasColumn(row, columnName) && !row.IsNull(columnName))
                {
                    return Convert.ToString(row[columnName]);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 讀取資料列中的整數欄位。
        /// </summary>
        /// <param name="row">資料列。</param>
        /// <param name="columnName">欄位名稱。</param>
        /// <returns>整數值。</returns>
        private static long GetDataTableInt64(DataRow row, string columnName)
        {
            long value;
            return long.TryParse(GetDataTableText(row, columnName), out value) ? value : 0;
        }

        /// <summary>
        /// 判斷資料列是否包含指定欄位。
        /// </summary>
        /// <param name="row">資料列。</param>
        /// <param name="columnName">欄位名稱。</param>
        /// <returns>是否包含欄位。</returns>
        private static bool HasColumn(DataRow row, string columnName)
        {
            return row.Table != null && row.Table.Columns.Contains(columnName);
        }

        /// <summary>
        /// 驗證格式名稱與欄位設定。
        /// </summary>
        /// <param name="request">待驗證的格式請求。</param>
        private static void ValidateRequest(ReconciliationIncludeTaxFormatSaveRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("格式資料不可為空白。");
            }

            if (string.IsNullOrWhiteSpace(request.FormatName))
            {
                throw new ArgumentException("請輸入格式名稱。");
            }

            if (request.FormatName.Trim().Length > 50)
            {
                throw new ArgumentException("格式名稱不可超過 50 個字元。");
            }

            if (request.Columns == null || !request.Columns.Any())
            {
                throw new ArgumentException("請至少設定一個匯出欄位。");
            }

            var fieldKeys = new HashSet<string>(
                FieldOptions.Select(x => x.Key),
                StringComparer.OrdinalIgnoreCase);
            foreach (var column in request.Columns)
            {
                if (column == null || string.IsNullOrWhiteSpace(column.ColumnName))
                {
                    throw new ArgumentException("匯出欄位名稱不可為空白。");
                }

                if (column.ColumnName.Trim().Length > 50)
                {
                    throw new ArgumentException("匯出欄位名稱不可超過 50 個字元。");
                }

                if (!Enum.IsDefined(typeof(ReconciliationIncludeTaxColumnSourceType), column.SourceType))
                {
                    throw new ArgumentException("匯出欄位資料來源類型不正確。");
                }

                if (column.SourceType == ReconciliationIncludeTaxColumnSourceType.Field)
                {
                    if (string.IsNullOrWhiteSpace(column.FieldKey) ||
                        !fieldKeys.Contains(column.FieldKey.Trim()))
                    {
                        throw new ArgumentException("匯出欄位尚未選擇有效的資料欄位。");
                    }
                }
                else if ((column.DefaultValue ?? string.Empty).Length > 200)
                {
                    throw new ArgumentException("欄位預設值不可超過 200 個字元。");
                }
            }
        }
    }
}
