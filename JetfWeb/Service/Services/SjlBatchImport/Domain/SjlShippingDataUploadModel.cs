using System;

namespace Service.Services.SjlBatchImport.Domain
{
    /// <summary>
    /// 捷利托運資料上傳資料列。
    /// </summary>
    public class SjlShippingDataUploadModel
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// 運送編號。
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 單據編號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 編號。
        /// </summary>
        public string Seq { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 派送日原始文字。
        /// </summary>
        public string DeliveryDateText { get; set; }

        /// <summary>
        /// 派送日。
        /// </summary>
        public DateTime? DeliveryDate { get; set; }

        /// <summary>
        /// 其他費用原始文字。
        /// </summary>
        public string OtherFeeText { get; set; }

        /// <summary>
        /// 其他費用。
        /// </summary>
        public decimal? OtherFee { get; set; }

        /// <summary>
        /// 代收原始文字。
        /// </summary>
        public string CodText { get; set; }

        /// <summary>
        /// 代收。
        /// </summary>
        public decimal? Cod { get; set; }

        /// <summary>
        /// 地址。
        /// </summary>
        public string ImporterAddr { get; set; }

        /// <summary>
        /// 品名。
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 件數原始文字。
        /// </summary>
        public string QtyText { get; set; }

        /// <summary>
        /// 件數。
        /// </summary>
        public int? Qty { get; set; }

        /// <summary>
        /// 材積原始文字。
        /// </summary>
        public string VolumeText { get; set; }

        /// <summary>
        /// 材積。
        /// </summary>
        public decimal? Volume { get; set; }

        /// <summary>
        /// 重量原始文字。
        /// </summary>
        public string GwText { get; set; }

        /// <summary>
        /// 重量。
        /// </summary>
        public decimal? Gw { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 上傳狀態。
        /// </summary>
        public string UploadStatus { get; set; }

        /// <summary>
        /// 失敗欄位名稱。
        /// </summary>
        public string FailFieldName { get; set; }

        /// <summary>
        /// 失敗原因。
        /// </summary>
        public string FailReason { get; set; }
    }
}
