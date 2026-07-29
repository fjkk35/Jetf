using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.ReconciliationIncludeTaxFormat.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Service.Services.ReconciliationIncludeTaxFormat
{
    /// <summary>
    /// 包稅客戶 Excel 匯出格式管理服務。
    /// </summary>
    public sealed class ReconciliationIncludeTaxFormatService : _BaseService
    {
        private static readonly IReadOnlyList<ReconciliationIncludeTaxFieldOption> FieldOptions =
            Enum.GetValues(typeof(ReconciliationIncludeTaxField))
                .Cast<ReconciliationIncludeTaxField>()
                .Select(field => new ReconciliationIncludeTaxFieldOption
                {
                    Key = field.ToFieldKey(),
                    Name = field.ToDisplayName()
                })
                .ToList();

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
                    Name = x.Name
                })
                .ToList();
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
                ColumnName = entity.ColumnName,
                SourceType = entity.SourceType,
                FieldKey = entity.FieldKey,
                DefaultValue = entity.DefaultValue
            };
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
