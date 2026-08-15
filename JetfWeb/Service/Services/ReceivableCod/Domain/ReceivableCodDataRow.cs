using System;

namespace Service.Services.ReceivableCod.Domain
{
    /// <summary>
    /// 到付款應收未收明細的資料庫投影資料。
    /// </summary>
    internal sealed class ReceivableCodDataRow
    {
        /// <summary>
        /// 資料識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 客戶類型，值為 AIR 或 SEA。
        /// </summary>
        public string CustomerType { get; set; }

        /// <summary>
        /// 報關資料類型。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 客戶代號。
        /// </summary>
        public string CustomerCode { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        public DateTime SignOutTime { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public decimal CodAmount { get; set; }

        /// <summary>
        /// 運費。
        /// </summary>
        public decimal FreightFee { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public decimal Fee { get; set; }

        /// <summary>
        /// 到付款應收金額。
        /// </summary>
        public decimal ReceivableAmount { get; set; }

        /// <summary>
        /// 已收金額。
        /// </summary>
        public int ReceivedAmount { get; set; }
    }
}
