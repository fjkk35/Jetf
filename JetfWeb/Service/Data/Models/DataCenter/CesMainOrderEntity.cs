using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// CES 主號彙總資料。
    /// </summary>
    [Table("CES_MAIN_ORDER", Schema = "dbo")]
    public sealed class CesMainOrderEntity
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
        /// 資料類型。
        /// </summary>
        [Column("TYPE")]
        public string Type { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAIN_NUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("CUST_CODE")]
        public string CustCode { get; set; }

        /// <summary>
        /// 資料日期。
        /// </summary>
        [Column("FIELD_DATE")]
        public DateTime? FieldDate { get; set; }

        /// <summary>
        /// 自訂欄位 A。
        /// </summary>
        [Column("FIELD_A")]
        public string FieldA { get; set; }

        /// <summary>
        /// 自訂欄位 B。
        /// </summary>
        [Column("FIELD_B")]
        public string FieldB { get; set; }

        /// <summary>
        /// 自訂欄位 C。
        /// </summary>
        [Column("FIELD_C")]
        public string FieldC { get; set; }

        /// <summary>
        /// 自訂欄位 D。
        /// </summary>
        [Column("FIELD_D")]
        public string FieldD { get; set; }

        /// <summary>
        /// 自訂欄位 E。
        /// </summary>
        [Column("FIELD_E")]
        public string FieldE { get; set; }

        /// <summary>
        /// 自訂欄位 F。
        /// </summary>
        [Column("FIELD_F")]
        public string FieldF { get; set; }

        /// <summary>
        /// 自訂欄位 G。
        /// </summary>
        [Column("FIELD_G")]
        public string FieldG { get; set; }

        /// <summary>
        /// 袋數。
        /// </summary>
        [Column("COUNT_BAG")]
        public int? CountBag { get; set; }

        /// <summary>
        /// 明細筆數。
        /// </summary>
        [Column("COUNT_RECORD")]
        public int? CountRecord { get; set; }

        /// <summary>
        /// 重量總和。
        /// </summary>
        [Column("SUM_WEIGHT")]
        public double? SumWeight { get; set; }

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
        /// 執行時間。
        /// </summary>
        [Column("EXECUTE_TIME")]
        public DateTime? ExecuteTime { get; set; }

        /// <summary>
        /// 版本號。
        /// </summary>
        [Column("VERSION")]
        public int? Version { get; set; }

        /// <summary>
        /// 是否為正式資料。
        /// </summary>
        [Column("IS_REAL")]
        public string IsReal { get; set; }

        /// <summary>
        /// 清關承辦代碼。
        /// </summary>
        [Column("CLEARANCE_CP")]
        public string ClearanceCp { get; set; }

        /// <summary>
        /// 上傳客戶代碼。
        /// </summary>
        [Column("UPLOAD_CUST")]
        public string UploadCust { get; set; }

        /// <summary>
        /// 上傳清關承辦代碼。
        /// </summary>
        [Column("UPLOAD_CP")]
        public string UploadCp { get; set; }
    }
}