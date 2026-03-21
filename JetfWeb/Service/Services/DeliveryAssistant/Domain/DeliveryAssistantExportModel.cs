using System;

namespace Service.Services.DeliveryAssistant.Domain
{
    public class DeliveryAssistantExportModel
    {
        /// <summary>
        /// 上傳時間
        /// </summary>
        public DateTime? UploadTime { get; set; }

        /// <summary>
        /// 客戶單號（Data）
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// 聯絡人（IMPORTER）
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 連絡電話（ImporterPhone）
        /// </summary>
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 住址（IM_ADD）
        /// </summary>
        public string ImporterAddr { get; set; }

        /// <summary>
        /// 重量（GW）
        /// </summary>
        public decimal? GW { get; set; }

        /// <summary>
        /// 應收款（TO_DLV_COD）
        /// </summary>
        public decimal? TO_DLV_COD { get; set; }
    }
}
