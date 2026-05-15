using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// CNO 預先申報回拋資料。
    /// </summary>
    [Table("ETL_CNO_PRE_DECLARE_CALLBACK", Schema = "dbo")]
    public sealed class EtlCnoPreDeclareCallbackEntity
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
        [Column("DATA_MODEL")]
        public string DataModel { get; set; }

        /// <summary>
        /// LP 代碼。
        /// </summary>
        [Column("LP_CODE")]
        public string LpCode { get; set; }

        /// <summary>
        /// 參數檢核結果。
        /// </summary>
        [Column("PARAM_CHECK_RESULT")]
        public string ParamCheckResult { get; set; }

        /// <summary>
        /// 參數錯誤訊息。
        /// </summary>
        [Column("PARAM_ERROR_MSG")]
        public string ParamErrorMsg { get; set; }

        /// <summary>
        /// 是否需要預先申報。
        /// </summary>
        [Column("NEED_PRE_DECLARE")]
        public string NeedPreDeclare { get; set; }

        /// <summary>
        /// 處理時間字串。
        /// </summary>
        [Column("OPT_TIME")]
        public string OptTime { get; set; }

        /// <summary>
        /// 預申報流水號。
        /// </summary>
        [Column("PRE_DECLARE_SEQ_NO")]
        public string PreDeclareSeqNo { get; set; }

        /// <summary>
        /// 預申報結果。
        /// </summary>
        [Column("PRE_DECLARE_RESULT")]
        public string PreDeclareResult { get; set; }

        /// <summary>
        /// 預申報訊息。
        /// </summary>
        [Column("PRE_DECLARE_MSG")]
        public string PreDeclareMsg { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("REMARK")]
        public string Remark { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CREATE_TIME")]
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFY_TIME")]
        public DateTime? ModifyTime { get; set; }

        /// <summary>
        /// 是否成功。
        /// </summary>
        [Column("SUCCESS")]
        public string Success { get; set; }

        /// <summary>
        /// 錯誤代碼。
        /// </summary>
        [Column("ERROR_CODE")]
        public string ErrorCode { get; set; }

        /// <summary>
        /// 錯誤訊息。
        /// </summary>
        [Column("ERROR_MSG")]
        public string ErrorMsg { get; set; }

        /// <summary>
        /// 回拋類型。
        /// </summary>
        [Column("TYPE")]
        public string Type { get; set; }
    }
}