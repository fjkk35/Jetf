using Service.EnumTax;
using Service.Extensions;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳上傳結果明細。
    /// </summary>
    public sealed class ReconciliationLogisticsResultItem
    {
        /// <summary>
        /// 回款日期。
        /// </summary>
        public string RepaymentDate { get; set; }

        /// <summary>
        /// 物流公司名稱。
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 費用主檔 TO_DLV_COD 或到付款資料 CC 的應收金額。
        /// </summary>
        public int ReceivableAmount { get; set; }

        /// <summary>
        /// 物流公司回款金額。
        /// </summary>
        public int RepaymentAmount { get; set; }

        /// <summary>
        /// 應收金額減去回款金額的差異。
        /// </summary>
        public int Difference { get; set; }

        /// <summary>
        /// 物流貨號比對狀態。
        /// </summary>
        public ReconciliationLogisticsResultStatus Status { get; set; }

        /// <summary>
        /// 物流貨號比對狀態顯示文字。
        /// </summary>
        public string StatusName => Status.ToDescription();

        /// <summary>
        /// 是否已成功更新費用明細或到付款資料。
        /// </summary>
        public bool IsSuccess =>
            Status == ReconciliationLogisticsResultStatus.Matched ||
            Status == ReconciliationLogisticsResultStatus.RepaymentExceedsReceivable ||
            Status == ReconciliationLogisticsResultStatus.RepaymentLessThanReceivable;

        /// <summary>
        /// 是否需要列入異常明細及異常筆數。
        /// </summary>
        public bool IsException =>
            Status != ReconciliationLogisticsResultStatus.Matched;
    }
}
