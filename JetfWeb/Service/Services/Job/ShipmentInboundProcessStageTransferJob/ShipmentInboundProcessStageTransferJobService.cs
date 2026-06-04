using NLog;
using Service.EnumTax;
using Service.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Services.Job.ShipmentInboundProcessStageTransferJob
{
    /// <summary>
    /// 預先登記處理轉檔排程。
    /// </summary>
    public class ShipmentInboundProcessStageTransferJobService : _BaseService
    {
        public ShipmentInboundProcessStageTransferJobService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        private const string JobName = "預先登記處理轉檔排程";
        private const string TransferSourceValue = "預先登記處理";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 執行預先登記處理轉檔。
        /// Step 1: 查詢所有尚未匹配的預先登記資料。
        /// Step 2: 依 TrackingNo 一次查出所有對應的 ShipmentInbound。
        /// Step 3: 批次同步欄位、寫入編輯紀錄並標記已匹配。
        /// Step 4: 儲存異動資料；發生例外時寫入 NLog。
        /// </summary>
        public Task RunShipmentInboundProcessStageTransferJobAsync()
        {
            try
            {
                using (var tx = JetfDb.Database.BeginTransaction())
                {
                    // Step 1: 一次查出尚未匹配的 Stage 與對應未出庫 ShipmentInbound。
                    var pairs = GetPendingStageShipmentPairs(JetfDb);
                    if (!pairs.Any())
                    {
                        return Task.CompletedTask;
                    }

                    // Step 2: 將 Stage 欄位同步到 ShipmentInbound，寫入欄位編輯紀錄並標記已匹配。
                    var syncTime = DateTime.Now;
                    foreach (var pair in pairs)
                    {
                        var stage = pair.Stage;
                        var shipment = pair.Shipment;

                        var processOpe = stage.ProcessOpe;

                        AddEditHistories(JetfDb, shipment, stage, syncTime, processOpe);
                        ApplyStageValues(stage, shipment, syncTime, processOpe);

                        stage.IsMatch = true;
                        stage.MatchTimie = syncTime;
                    }

                    // Step 3: 儲存本次批次同步結果。
                    JetfDb.SaveChanges();
                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                WriteErrorLog("查詢待轉檔資料失敗", ex);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 查詢所有尚未匹配，且能對應到未出庫 ShipmentInbound 的 Stage/Shipment 組合。
        /// </summary>
        /// <param name="db">Jetf 資料庫內容。</param>
        /// <returns>待同步的 Stage 與 ShipmentInbound 配對清單。</returns>
        private List<PendingStageShipmentPair> GetPendingStageShipmentPairs(Data.JetfDbContext db)
        {
            return (
                from stage in db.ShipmentInboundProcessStages
                join shipment in db.ShipmentInbounds
                    on stage.TrackingNo equals shipment.TrackingNo
                where !stage.IsMatch
                    && shipment.OutboundTime == null
                orderby stage.Id
                select new PendingStageShipmentPair
                {
                    Stage = stage,
                    Shipment = shipment
                })
                .ToList();
        }

        /// <summary>
        /// 將 Stage 欄位值同步到正式 ShipmentInbound。
        /// </summary>
        /// <param name="stage">預先登記處理資料。</param>
        /// <param name="shipment">正式 ShipmentInbound 資料。</param>
        /// <param name="processTime">本次同步時間。</param>
        /// <param name="processOpe">本次同步處理人員。</param>
        private void ApplyStageValues(
            Data.ShipmentInboundProcessStageEntity stage,
            Data.ShipmentInboundEntity shipment,
            DateTime processTime,
            string processOpe)
        {
            // Step 3-1: 將預先登記處理欄位覆寫回正式 ShipmentInbound。
            shipment.ReturnReason = stage.ReturnReason;
            shipment.Remark = stage.Remark;

            if (!stage.ProcessType.HasValue)
            {
                return;
            }

            shipment.Fee = stage.Fee;
            shipment.Tax = stage.Tax;
            shipment.Ccfee = stage.CcFee;
            shipment.Cod = stage.Cod;
            shipment.ProcessType = stage.ProcessType;
            shipment.ProcessTransNo = stage.ProcessTransNo;
            shipment.ProcessImporter = stage.ProcessImporter;
            shipment.ProcessImporterPhone = stage.ProcessImporterPhone;
            shipment.ProcessImporterAddr = stage.ProcessImporterAddr;
            shipment.FreightPayerNo = stage.FreightPayerNo;
            shipment.FreightFee = stage.FreightFee;
            shipment.CarNo = stage.CarNo;
            shipment.StoreCode = stage.StoreCode;
            shipment.StoreName = stage.StoreName;
            shipment.PickupTime = stage.PickupTime;
            shipment.ProcessOpe = processOpe;
            shipment.ProcessTime = processTime;
        }

        /// <summary>
        /// 寫入欄位編輯紀錄；只有舊值非空且新舊值不同時才寫入。
        /// </summary>
        /// <param name="db">Jetf 資料庫內容。</param>
        /// <param name="shipment">正式 ShipmentInbound 資料。</param>
        /// <param name="stage">預先登記處理資料。</param>
        /// <param name="editTime">本次編輯時間。</param>
        /// <param name="processOpe">本次編輯人員。</param>
        private void AddEditHistories(
            Data.JetfDbContext db,
            Data.ShipmentInboundEntity shipment,
            Data.ShipmentInboundProcessStageEntity stage,
            DateTime editTime,
            string processOpe)
        {
            AddTransferSourceHistory(db, shipment.Id, editTime, processOpe);
            AddEditHistoryIfChanged(db, shipment.Id, "退件原因", shipment.ReturnReason, stage.ReturnReason, editTime, processOpe, value => value);
            AddEditHistoryIfChanged(db, shipment.Id, "備註", shipment.Remark, stage.Remark, editTime, processOpe, value => value);

            if (!stage.ProcessType.HasValue)
            {
                return;
            }

            AddEditHistoryIfChanged(db, shipment.Id, "手續費", shipment.Fee, stage.Fee, editTime, processOpe, value => value?.ToString());
            AddEditHistoryIfChanged(db, shipment.Id, "稅金", shipment.Tax, stage.Tax, editTime, processOpe, value => value?.ToString());
            AddEditHistoryIfChanged(db, shipment.Id, "報關費", shipment.Ccfee, stage.CcFee, editTime, processOpe, value => value?.ToString());
            AddEditHistoryIfChanged(db, shipment.Id, "到付款", shipment.Cod, stage.Cod, editTime, processOpe, value => value?.ToString());
            AddEditHistoryIfChanged(db, shipment.Id, "處理方式", shipment.ProcessType, stage.ProcessType, editTime, processOpe, GetProcessTypeText);
            AddEditHistoryIfChanged(db, shipment.Id, "重出派件公司", shipment.ProcessTransNo, stage.ProcessTransNo, editTime, processOpe, GetProcessTransNoText);
            AddEditHistoryIfChanged(db, shipment.Id, "收件人", shipment.ProcessImporter, stage.ProcessImporter, editTime, processOpe, value => value);
            AddEditHistoryIfChanged(db, shipment.Id, "電話", shipment.ProcessImporterPhone, stage.ProcessImporterPhone, editTime, processOpe, value => value);
            AddEditHistoryIfChanged(db, shipment.Id, "宅配地址", shipment.ProcessImporterAddr, stage.ProcessImporterAddr, editTime, processOpe, value => value);
            AddEditHistoryIfChanged(db, shipment.Id, "運費支付方", shipment.FreightPayerNo, stage.FreightPayerNo, editTime, processOpe, GetFreightPayerNoText);
            AddEditHistoryIfChanged(db, shipment.Id, "運費", shipment.FreightFee, stage.FreightFee, editTime, processOpe, value => value?.ToString());
            AddEditHistoryIfChanged(db, shipment.Id, "車牌號碼", shipment.CarNo, stage.CarNo, editTime, processOpe, value => value);
            AddEditHistoryIfChanged(db, shipment.Id, "門市店號", shipment.StoreCode, stage.StoreCode, editTime, processOpe, value => value);
            AddEditHistoryIfChanged(db, shipment.Id, "門市名稱", shipment.StoreName, stage.StoreName, editTime, processOpe, value => value);
            AddEditHistoryIfChanged(db, shipment.Id, "預計自取日期", shipment.PickupTime, stage.PickupTime, editTime, processOpe, GetDateText);
            AddEditHistoryIfChanged(db, shipment.Id, "客服處理人員", shipment.ProcessOpe, processOpe, editTime, processOpe, value => value);
        }

        /// <summary>
        /// 寫入預先登記處理轉檔來源紀錄，讓編輯紀錄可辨識資料來源。
        /// </summary>
        /// <param name="db">Jetf 資料庫內容。</param>
        /// <param name="shipmentInboundId">ShipmentInbound Id。</param>
        /// <param name="editTime">編輯時間。</param>
        /// <param name="editUser">編輯人員。</param>
        private void AddTransferSourceHistory(
            Data.JetfDbContext db,
            int shipmentInboundId,
            DateTime editTime,
            string editUser)
        {
            db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
            {
                ShipmentInboundId = shipmentInboundId,
                FieldName = TransferSourceValue,
                OldValue = string.Empty,
                NewValue = string.Empty,
                EditTime = editTime,
                EditUser = editUser
            });
        }

        /// <summary>
        /// 比較單一欄位的新舊值，符合條件時寫入 ShipmentInboundEditHistory。
        /// </summary>
        /// <typeparam name="T">欄位值型別。</typeparam>
        /// <param name="db">Jetf 資料庫內容。</param>
        /// <param name="shipmentInboundId">ShipmentInbound Id。</param>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="oldValue">舊值。</param>
        /// <param name="newValue">新值。</param>
        /// <param name="editTime">編輯時間。</param>
        /// <param name="editUser">編輯人員。</param>
        /// <param name="formatter">欄位值轉字串的方法。</param>
        private void AddEditHistoryIfChanged<T>(
            Data.JetfDbContext db,
            int shipmentInboundId,
            string fieldName,
            T oldValue,
            T newValue,
            DateTime editTime,
            string editUser,
            Func<T, string> formatter)
        {
            var oldText = formatter(oldValue);
            var newText = formatter(newValue);

            if (string.Equals(oldText, newText, StringComparison.Ordinal))
            {
                return;
            }

            db.ShipmentInboundEditHistories.Add(new Data.ShipmentInboundEditHistoryEntity
            {
                ShipmentInboundId = shipmentInboundId,
                FieldName = fieldName,
                OldValue = oldText,
                NewValue = newText,
                EditTime = editTime,
                EditUser = editUser
            });
        }

        /// <summary>
        /// 將處理方式代碼轉成顯示文字。
        /// </summary>
        /// <param name="value">處理方式代碼。</param>
        /// <returns>處理方式文字。</returns>
        private string GetProcessTypeText(ShipmentInboundProcessType? value)
        {
            return value?.ToDescription();
        }

        /// <summary>
        /// 將重出派件公司代碼轉成顯示文字。
        /// </summary>
        /// <param name="value">重出派件公司代碼。</param>
        /// <returns>重出派件公司文字。</returns>
        private string GetProcessTransNoText(byte? value)
        {
            return value.HasValue
                ? ((ShipmentInboundProcessTransNo)value.Value).ToDescription()
                : string.Empty;
        }

        /// <summary>
        /// 將運費支付方代碼轉成顯示文字。
        /// </summary>
        /// <param name="value">運費支付方代碼。</param>
        /// <returns>運費支付方文字。</returns>
        private string GetFreightPayerNoText(byte? value)
        {
            return value.HasValue
                ? ((ShipmentInboundFreightPayerNo)value.Value).ToDescription()
                : string.Empty;
        }

        /// <summary>
        /// 將日期欄位格式化為 yyyy/MM/dd。
        /// </summary>
        /// <param name="value">日期值。</param>
        /// <returns>格式化後的日期文字。</returns>
        private string GetDateText(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToString("yyyy/MM/dd")
                : string.Empty;
        }

        /// <summary>
        /// 將排程例外寫入 NLog。
        /// </summary>
        /// <param name="context">錯誤發生的上下文。</param>
        /// <param name="ex">例外內容。</param>
        private void WriteErrorLog(string context, Exception ex)
        {
            try
            {
                Logger.Error(ex, BuildLogMessage(context));
            }
            catch
            {
            }
        }

        /// <summary>
        /// 組合排程錯誤訊息。
        /// </summary>
        /// <param name="context">錯誤發生的上下文。</param>
        /// <returns>要寫入 log 的訊息文字。</returns>
        private string BuildLogMessage(string context)
        {
            var message = $"{JobName} | {context}";
            return message.Length <= 1000
                ? message
                : message.Substring(0, 1000);
        }

        /// <summary>
        /// 預先登記處理轉檔的 Stage 與 ShipmentInbound 配對資料。
        /// </summary>
        private sealed class PendingStageShipmentPair
        {
            public Data.ShipmentInboundProcessStageEntity Stage { get; set; }

            public Data.ShipmentInboundEntity Shipment { get; set; }
        }
    }
}
