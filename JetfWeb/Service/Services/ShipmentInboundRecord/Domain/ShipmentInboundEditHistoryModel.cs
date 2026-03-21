using System;

namespace Service.Services.ShipmentInboundRecord.Domain
{
    /// <summary>
    /// 貨件入庫編輯歷史記錄
    /// </summary>
    public class ShipmentInboundEditHistoryModel
    {
        /// <summary>
        /// 主鍵 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 貨件入庫 Id
        /// </summary>
        public int ShipmentInboundId { get; set; }

        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 欄位名稱（中文顯示）
        /// </summary>
        public string FieldNameText
        {
            get
            {
                switch (FieldName)
                {
                    case "Cod":
                        return "到付款";
                    case "Tax":
                        return "稅金";
                    case "Ccfee":
                        return "報關費";
                    default:
                        return FieldName;
                }
            }
        }

        /// <summary>
        /// 舊值
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// 新值
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// 編輯時間
        /// </summary>
        public DateTime EditTime { get; set; }

        /// <summary>
        /// 編輯人員
        /// </summary>
        public string EditUser { get; set; }
    }
}
