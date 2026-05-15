using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 系統參數主檔。
    /// </summary>
    [Table("SYS_PARAM", Schema = "dbo")]
    public sealed class SysParamEntity
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
        /// 參數代碼。
        /// </summary>
        [Column("CODE")]
        public string Code { get; set; }

        /// <summary>
        /// 參數分類。
        /// </summary>
        [Column("TYPE")]
        public string Type { get; set; }

        /// <summary>
        /// 參數名稱。
        /// </summary>
        [Column("NAME")]
        public string Name { get; set; }

        /// <summary>
        /// 參數描述。
        /// </summary>
        [Column("DESC")]
        public string Description { get; set; }

        /// <summary>
        /// 備註說明。
        /// </summary>
        [Column("NOTE")]
        public string Note { get; set; }

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
        /// 額外備註。
        /// </summary>
        [Column("MEMO")]
        public string Memo { get; set; }

        /// <summary>
        /// 排序序號。
        /// </summary>
        [Column("SEQUENCE")]
        public int? Sequence { get; set; }

        /// <summary>
        /// 整數值。
        /// </summary>
        [Column("VALUE")]
        public int? Value { get; set; }
    }
}