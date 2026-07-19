using System.ComponentModel;

namespace Service.EnumTax
{
    /// <summary>
    /// 應收款項收取對象。
    /// </summary>
    public enum ReceivableCollectionType
    {
        /// <summary>
        /// 向廠商收取。
        /// </summary>
        [Description("跟廠商收")]
        Customer = 1,

        /// <summary>
        /// 向派件公司收取。
        /// </summary>
        [Description("跟派件收")]
        Trans = 2
    }
}
