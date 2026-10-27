using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 貨物處理紀錄的查重與批次新增資料，查詢僅投影單號及類型且不追蹤實體。
    /// </summary>
    [Table("Process", Schema = "dbo")]
    public sealed class ProcessEntity
    {
        /// <summary>
        /// 處理紀錄資料日期，格式為 yyyyMMdd。
        /// </summary>
        [Column("DATADATE")]
        public string DataDate { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Key]
        [Column("DLV_INV", Order = 0)]
        public string DlvInv { get; set; }

        /// <summary>
        /// 處理備註。
        /// </summary>
        [Column("REMARK")]
        public string Remark { get; set; }

        /// <summary>
        /// 附件路徑，本匯入流程使用空字串。
        /// </summary>
        [Column("FILEPATH")]
        public string FilePath { get; set; }

        /// <summary>
        /// 附件檔名，本匯入流程使用空字串。
        /// </summary>
        [Column("FILENAME")]
        public string FileName { get; set; }

        /// <summary>
        /// 上傳人員代碼。
        /// </summary>
        [Column("USER_ID")]
        public string UserId { get; set; }

        /// <summary>
        /// 處理類型。
        /// </summary>
        [Key]
        [Column("PROCESS_TYPE", Order = 1)]
        public string ProcessType { get; set; }
    }
}
