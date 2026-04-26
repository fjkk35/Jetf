using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 派件公司主檔資料。
    /// </summary>
    [Table("customer_master", Schema = "dbo")]
    public sealed class CustomerMasterEntity
    {
        /// <summary>
        /// 派件公司代碼。
        /// </summary>
        [Key]
        [Column("TRANS_NO")]
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        [Column("TRANS_NAME")]
        public string TransName { get; set; }

        /// <summary>
        /// 運輸類型。
        /// </summary>
        [Column("TRAN_TYPE")]
        public string TranType { get; set; }
    }
}
