using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 明細實體，對應資料表 jetf.dbo.SeaClearanceDetail
    /// </summary>
    [Table("SeaClearanceDetail", Schema = "dbo")]
    public sealed class SeaClearanceDetailEntity
    {
        /// <summary>
        /// 主鍵：SeaClearanceDetail Id
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 所屬的 SeaClearance 主表 Id（可為 null）
        /// </summary>
        [Column("SeaClearanceId")]
        public int? SeaClearanceId { get; set; }

        /// <summary>
        /// 資料日期
        /// </summary>
        [Column("DataDate")]
        public string DataDate { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        [Column("MainNumber")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 追蹤編號
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        [Column("MftNo")]
        public string MftNo { get; set; }

        [Column("Memo")]
        public string Memo { get; set; }

        [Column("ImportDate")]
        public string ImportDate { get; set; }

        /// <summary>
        /// Gb301-報單號碼
        /// </summary>
        [Column("DeclNo")]
        public string DeclNo { get; set; }

        [Column("ProDateTime")]
        public DateTime? ProDateTime { get; set; }

        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }

        [Column("IsSeaOrderOriginal")]
        public bool? IsSeaOrderOriginal { get; set; }

        [Column("Tax")]
        public int? Tax { get; set; }

        [Column("CustomsBrokerId")]
        public int? CustomsBrokerId { get; set; }

        [Column("CustomsBrokerageId")]
        public int? CustomsBrokerageId { get; set; }

        [Column("SignInTime")]
        public DateTime? SignInTime { get; set; }

        /// <summary>
        /// 出倉日期
        /// </summary>
        [Column("SignOutTime")]
        public DateTime? SignOutTime { get; set; }

        [Column("ContactEmail")]
        public string ContactEmail { get; set; }

        [Column("ContactChangeData")]
        public string ContactChangeData { get; set; }

        /// <summary>
        /// 步驟Id（可為 null）
        /// </summary>
        [Column("CurrentStepId")]
        public int? CurrentStepId { get; set; }

        /// <summary>
        /// 異常狀態Id（可為 null）
        /// </summary>
        [Column("CurrentAbnormalStateId")]
        public int? CurrentAbnormalStateId { get; set; }

        [Column("IsCustomsHold")]
        public bool? IsCustomsHold { get; set; }

        [Column("CustomsHold")]
        public string CustomsHold { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        [Column("IsSucess")]
        public bool IsSucess { get; set; }
    }
}