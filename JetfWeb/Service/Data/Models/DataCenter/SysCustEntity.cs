using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// DATA_CENTER 客戶主檔資料。
    /// </summary>
    [Table("SYS_CUST", Schema = "dbo")]
    public sealed class SysCustEntity
    {
        /// <summary>
        /// 資料流水號。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 資料模式。
        /// </summary>
        [Column("MODEL")]
        public string Model { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("CUST_CODE")]
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶類型。
        /// </summary>
        [Column("CUST_TYPE")]
        public string CustType { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        [Column("CUST_NAME")]
        public string CustName { get; set; }

        /// <summary>
        /// 客戶統編或證號。
        /// </summary>
        [Column("CUST_ID")]
        public string CustId { get; set; }

        /// <summary>
        /// 客戶聯絡人。
        /// </summary>
        [Column("CUST_CONTACT")]
        public string CustContact { get; set; }

        /// <summary>
        /// 客戶別名。
        /// </summary>
        [Column("CUST_ALIAS")]
        public string CustAlias { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CREATE_TIME")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFY_TIME")]
        public DateTime? ModifyTime { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("MEMO")]
        public string Memo { get; set; }

        /// <summary>
        /// 排序序號。
        /// </summary>
        [Column("SEQUENCE")]
        public int? Sequence { get; set; }

        /// <summary>
        /// 舊客戶代碼。
        /// </summary>
        [Column("OLD_CODE")]
        public string OldCode { get; set; }

        /// <summary>
        /// 結案類型。
        /// </summary>
        [Column("CLOSE_TYPE")]
        public string CloseType { get; set; }

        /// <summary>
        /// 併單代碼。
        /// </summary>
        [Column("CONSOL_CODE")]
        public string ConsolCode { get; set; }

        /// <summary>
        /// 併單類型。
        /// </summary>
        [Column("CONSOL_TYPE")]
        public string ConsolType { get; set; }

        /// <summary>
        /// 併單名稱。
        /// </summary>
        [Column("CONSOL_NAME")]
        public string ConsolName { get; set; }

        /// <summary>
        /// 併單網址。
        /// </summary>
        [Column("CONSOL_URL")]
        public string ConsolUrl { get; set; }
    }
}
