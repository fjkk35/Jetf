using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 貨態明細資料。
    /// </summary>
    [Table("CARGO_STATUS_DETAIL", Schema = "dbo")]
    public sealed class CargoStatusDetailEntity
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
        /// 轉運單號。
        /// </summary>
        [Column("TRANS_NUMBER")]
        public string TransNumber { get; set; }

        /// <summary>
        /// 轉運狀態代碼。
        /// </summary>
        [Column("TRANS_STATUS")]
        public string TransStatus { get; set; }

        /// <summary>
        /// 轉運狀態說明。
        /// </summary>
        [Column("TRANS_STATUS_DESC")]
        public string TransStatusDesc { get; set; }

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
        /// 位置資訊。
        /// </summary>
        [Column("LOCATION")]
        public string Location { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 關聯主資料流水號。
        /// </summary>
        [Column("RELATE_ROW_ID")]
        public int? RelateRowId { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CREATE_TIME")]
        public DateTime? CreateTime { get; set; }
    }
}