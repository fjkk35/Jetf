using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 明細對應至原始訂單的映射紀錄，對應資料表 jetf.dbo.SeaClearanceDetailOriginalMapping
    /// </summary>
    [Table("SeaClearanceDetailOriginalMapping", Schema = "dbo")]
    public sealed class SeaClearanceDetailOriginalMappingEntity
    {
        /// <summary>
        /// 對應的 SeaClearanceDetail Id（複合主鍵之一）
        /// </summary>
        [Key]
        [Column("SeaClearanceDetailId", Order = 0)]
        public int SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 原始訂單 Id（複合主鍵之一）
        /// </summary>
        [Key]
        [Column("SeaOrderOriginalId", Order = 1)]
        public int SeaOrderOriginalId { get; set; }

        /// <summary>
        /// 原單上傳日期
        /// </summary>
        [Column("CreateDate")]
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// 倉別
        /// </summary>
        [Column("Modifyby")]
        public string Modifyby { get; set; }

        /// <summary>
        /// 報關方式
        /// </summary>
        [Column("Post_Entry")]
        public string Post_Entry { get; set; }

        /// <summary>
        /// 預計到港日
        /// </summary>
        [Column("Eta")]
        public DateTime? Eta { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        [Column("Cust_Code")]
        public string Cust_Code { get; set; }

        /// <summary>
        /// 件數
        /// </summary>
        [Column("Piece")]
        public int? Piece { get; set; }

        /// <summary>
        /// 原單申報人
        /// </summary>
        [Column("Importer")]
        public string Importer { get; set; }

        [Column("Im_Phoneno")]
        public string Im_Phoneno { get; set; }

        [Column("Importer_Id")]
        public string Importer_Id { get; set; }

        [Column("Tax_Payment")]
        public string Tax_Payment { get; set; }

        /// <summary>
        /// 派件
        /// </summary>
        [Column("Jetf_Serial")]
        public string Jetf_Serial { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
        [Column("Item_Name")]
        public string Item_Name { get; set; }

        /// <summary>
        /// 重量（Gw）
        /// </summary>
        [Column("Gw")]
        public decimal? Gw { get; set; }

        [Column("CC")]
        public double? CC { get; set; }
    }
}
