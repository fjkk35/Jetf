using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// PLINK 錯誤資料。
    /// </summary>
    [Table("ETL_PLINK_ERROR", Schema = "dbo")]
    public sealed class EtlPlinkErrorEntity
    {
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        [Column("DEP")]
        public string Dep { get; set; }

        [Column("DECL_NO")]
        public string DeclNo { get; set; }

        [Column("DATA_NO")]
        public string DataNo { get; set; }

        [Column("MAWB")]
        public string Mawb { get; set; }

        [Column("HAWB")]
        public string Hawb { get; set; }

        [Column("BAG_NO")]
        public string BagNo { get; set; }

        [Column("CUST")]
        public string Cust { get; set; }

        [Column("REASON")]
        public string Reason { get; set; }

        [Column("OUT_TIME")]
        public DateTime? OutTime { get; set; }

        [Column("TAX")]
        public int? Tax { get; set; }

        [Column("CREATE_TIME")]
        public DateTime? CreateTime { get; set; }

        [Column("STATUS")]
        public string Status { get; set; }

        [Column("ISSUEDATE")]
        public DateTime? IssueDate { get; set; }

        [Column("PRO_TYPE")]
        public string ProType { get; set; }

        [Column("PRO_DATE")]
        public string ProDate { get; set; }

        [Column("CLEARANCE_TYPE")]
        public string ClearanceType { get; set; }
    }
}