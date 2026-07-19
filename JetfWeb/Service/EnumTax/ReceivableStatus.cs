using System.ComponentModel;

namespace Service.EnumTax
{
    /// <summary>
    /// 應收款項回收狀態。
    /// </summary>
    public enum ReceivableStatus
    {
        /// <summary>
        /// 已收回任一筆客戶或派件公司款項。
        /// </summary>
        [Description("已收回")]
        Received = 1,

        /// <summary>
        /// 客戶及派件公司款項皆尚未收回。
        /// </summary>
        [Description("未收回")]
        Unreceived = 2
    }
}
