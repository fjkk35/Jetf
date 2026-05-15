using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 派送來源主檔。
    /// </summary>
    [Table("DESPATCHFROM", Schema = "dbo")]
    public sealed class DespatchFromEntity
    {
        /// <summary>
        /// 資料主鍵。
        /// </summary>
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>
        /// 派送來源代碼。
        /// </summary>
        [Column("DESPATCHNO")]
        public string DespatchNo { get; set; }

        /// <summary>
        /// 派送來源名稱。
        /// </summary>
        [Column("DESPATCHNAME")]
        public string DespatchName { get; set; }

        /// <summary>
        /// 派送來源稅籍編號。
        /// </summary>
        [Column("DESPATCHTAXNO")]
        public string DespatchTaxNo { get; set; }

        /// <summary>
        /// 派送來源別名。
        /// </summary>
        [Column("DESPATCHALIAS")]
        public string DespatchAlias { get; set; }
    }
}