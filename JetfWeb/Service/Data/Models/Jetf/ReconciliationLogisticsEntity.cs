using Service.EnumTax;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 物流銷帳上傳資料。
    /// </summary>
    [Table("ReconciliationLogistics", Schema = "dbo")]
    public sealed class ReconciliationLogisticsEntity
    {
        /// <summary>
        /// 主鍵。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 物流公司。
        /// </summary>
        public ReconciliationLogisticsCompany Company { get; set; }

        /// <summary>
        /// 本次回款日期。
        /// </summary>
        [Column(TypeName = "date")]
        public DateTime RepaymentDate { get; set; }

        /// <summary>
        /// 分提單號。
        /// <para>新竹物流清單格式：清單編號。</para>
        /// <para>新竹物流匯款明細格式：出貨單號。</para>
        /// <para>7-11：訂單號碼。</para>
        /// <para>客樂得：訂單號碼。</para>
        /// <para>大榮：出貨單號。</para>
        /// <para>超峰：订单号。</para>
        /// <para>圓通：原單號。</para>
        /// <para>關貿：分提單號碼。</para>
        /// <para>全家：不使用此欄位。</para>
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// <para>新竹物流清單格式：查貨號碼。</para>
        /// <para>新竹物流匯款明細格式：宅配單號。</para>
        /// <para>7-11：出貨單號。</para>
        /// <para>客樂得及超峰：託運單號。</para>
        /// <para>大榮：移除「空白＋00」後綴的明細單號。</para>
        /// <para>現金：運單號。</para>
        /// <para>圓通：圆通单号。</para>
        /// <para>關貿：不使用此欄位。</para>
        /// <para>全家：廠商訂單編號。</para>
        /// </summary>
        [Required]
        [StringLength(50)]
        public string DlvInv { get; set; }

        /// <summary>
        /// 回收金額。
        /// <para>新竹物流清單格式：代收貨款金額。</para>
        /// <para>新竹物流匯款明細格式：現金金額。</para>
        /// <para>7-11：訂單金額。</para>
        /// <para>客樂得及大榮：實收金額。</para>
        /// <para>超峰：應收金額。</para>
        /// <para>現金：金額。</para>
        /// <para>圓通：合计。</para>
        /// <para>關貿：交易金額。</para>
        /// <para>全家：代收金額。</para>
        /// </summary>
        public int ReceivedAmount { get; set; }

        /// <summary>
        /// 應收金額減去回款金額的差異；查無物流貨號時為 0。
        /// </summary>
        public int DifferenceAmount { get; set; }

        /// <summary>
        /// 新竹物流客戶代號或客戶別。
        /// </summary>
        [StringLength(20)]
        public string CustomerCode { get; set; }

        /// <summary>
        /// 是否成功更新至少一筆 FEE_MASTER_DETAIL。
        /// </summary>
        public bool IsFeeMaster { get; set; }

        /// <summary>
        /// 是否成功更新一筆 FEE_MASTER_COD。
        /// </summary>
        public bool IsFeeMasterCod { get; set; }

        /// <summary>
        /// 物流銷帳比對結果狀態；既有未回填狀態的資料為 null。
        /// </summary>
        public ReconciliationLogisticsResultStatus? Status { get; set; }

        /// <summary>
        /// 原始上傳檔名。
        /// </summary>
        [Required]
        [StringLength(255)]
        public string SourceFileName { get; set; }

        /// <summary>
        /// 上傳操作人員。
        /// </summary>
        [Required]
        [StringLength(10)]
        public string CreatedUserId { get; set; }

        /// <summary>
        /// 上傳時間。
        /// </summary>
        public DateTime CreatedTime { get; set; }
    }
}
