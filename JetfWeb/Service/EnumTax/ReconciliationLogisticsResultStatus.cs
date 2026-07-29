using System.ComponentModel;

namespace Service.EnumTax
{
    /// <summary>
    /// 物流銷帳資料比對狀態。
    /// </summary>
    public enum ReconciliationLogisticsResultStatus
    {
        /// <summary>
        /// 查無符合的物流貨號。
        /// </summary>
        [Description("查無物流貨號")]
        FeeMasterNotFound = 0,

        /// <summary>
        /// 已成功比對費用主檔。
        /// </summary>
        [Description("比對成功")]
        Matched = 1,

        /// <summary>
        /// 回款金額大於費用明細應收金額。
        /// </summary>
        [Description("回款金額大於明細應收金額")]
        RepaymentExceedsReceivable = 2,

        /// <summary>
        /// 已比對費用主檔，但沒有可銷帳金額。
        /// </summary>
        [Description("無可銷帳金額")]
        NoReceivableAmount = 3,

        /// <summary>
        /// 回款金額小於費用明細應收金額。
        /// </summary>
        [Description("回款金額小於明細應收金額")]
        RepaymentLessThanReceivable = 4,

        /// <summary>
        /// 物流貨號仍對應多筆費用資料。
        /// </summary>
        [Description("物流貨號重複")]
        DlvInvDuplicate = 5
    }
}
