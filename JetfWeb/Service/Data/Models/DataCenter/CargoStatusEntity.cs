using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 貨態彙總資料。
    /// </summary>
    [Table("CARGO_STATUS", Schema = "dbo")]
    public sealed class CargoStatusEntity
    {
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// JETF 序號。
        /// </summary>
        [Column("JETF_SERIAL")]
        public string JetfSerial { get; set; }

        /// <summary>
        /// JETF 貨態代碼。
        /// </summary>
        [Column("JETF_STATUS")]
        public string JetfStatus { get; set; }

        /// <summary>
        /// JETF 貨態說明。
        /// </summary>
        [Column("JETF_STATUS_DESC")]
        public string JetfStatusDesc { get; set; }

        /// <summary>
        /// 原始系統代碼。
        /// </summary>
        [Column("ORIGIN_CODE")]
        public string OriginCode { get; set; }

        /// <summary>
        /// 原始系統名稱。
        /// </summary>
        [Column("ORIGIN_NAME")]
        public string OriginName { get; set; }

        /// <summary>
        /// 原始系統序號。
        /// </summary>
        [Column("ORIGIN_SERIAL")]
        public string OriginSerial { get; set; }

        /// <summary>
        /// 原始系統貨態代碼。
        /// </summary>
        [Column("ORIGIN_STATUS")]
        public string OriginStatus { get; set; }

        /// <summary>
        /// 原始系統貨態說明。
        /// </summary>
        [Column("ORIGIN_STATUS_DESC")]
        public string OriginStatusDesc { get; set; }

        /// <summary>
        /// 轉運代碼。
        /// </summary>
        [Column("TRANS_CODE")]
        public string TransCode { get; set; }

        /// <summary>
        /// 轉運名稱。
        /// </summary>
        [Column("TRANS_NAME")]
        public string TransName { get; set; }

        /// <summary>
        /// 轉運序號。
        /// </summary>
        [Column("TRANS_SERIAL")]
        public string TransSerial { get; set; }

        /// <summary>
        /// 轉運貨態代碼。
        /// </summary>
        [Column("TRANS_STATUS")]
        public string TransStatus { get; set; }

        /// <summary>
        /// 轉運貨態說明。
        /// </summary>
        [Column("TRANS_STATUS_DESC")]
        public string TransStatusDesc { get; set; }

        /// <summary>
        /// 資料狀態。
        /// </summary>
        [Column("DATA_STATUS")]
        public string DataStatus { get; set; }

        /// <summary>
        /// 資料建立時間。
        /// </summary>
        [Column("DATA_CREATE_TIME")]
        public DateTime DataCreateTime { get; set; }

        /// <summary>
        /// JETF 更新時間。
        /// </summary>
        [Column("JETF_MODIFY_TIME")]
        public DateTime? JetfModifyTime { get; set; }

        /// <summary>
        /// 原始系統更新時間。
        /// </summary>
        [Column("ORIGIN_MODIFY_TIME")]
        public DateTime? OriginModifyTime { get; set; }

        /// <summary>
        /// 轉運更新時間。
        /// </summary>
        [Column("TRANS_MODIFY_TIME")]
        public DateTime? TransModifyTime { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAGNO")]
        public string BagNo { get; set; }

        /// <summary>
        /// 執行時間。
        /// </summary>
        [Column("EXECUTE_TIME")]
        public DateTime ExecuteTime { get; set; }

        /// <summary>
        /// 是否為訂單。
        /// </summary>
        [Column("IS_ORDER")]
        public string IsOrder { get; set; }
    }
}