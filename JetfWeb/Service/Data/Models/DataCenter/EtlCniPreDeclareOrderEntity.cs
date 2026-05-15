using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// CNI 預先申報訂單資料。
    /// </summary>
    [Table("ETL_CNI_PRE_DECLARE_ORDER", Schema = "dbo")]
    public sealed class EtlCniPreDeclareOrderEntity
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
        /// 訊息類型。
        /// </summary>
        [Column("MSG_TYPE")]
        public string MsgType { get; set; }

        /// <summary>
        /// 訊息識別碼。
        /// </summary>
        [Column("MSG_ID")]
        public string MsgId { get; set; }

        /// <summary>
        /// 發送方代碼。
        /// </summary>
        [Column("FROM_CODE")]
        public string FromCode { get; set; }

        /// <summary>
        /// 合作夥伴代碼。
        /// </summary>
        [Column("PARTNER_CODE")]
        public string PartnerCode { get; set; }

        /// <summary>
        /// 是否拆單。
        /// </summary>
        [Column("IS_SPLIT")]
        public string IsSplit { get; set; }

        /// <summary>
        /// LP 代碼。
        /// </summary>
        [Column("LP_CODE")]
        public string LpCode { get; set; }

        /// <summary>
        /// 子 LP 代碼清單。
        /// </summary>
        [Column("SUB_LP_CODES")]
        public string SubLpCodes { get; set; }

        /// <summary>
        /// 毛重。
        /// </summary>
        [Column("GROSS_WEIGHT")]
        public decimal? GrossWeight { get; set; }

        /// <summary>
        /// 淨重。
        /// </summary>
        [Column("NET_WEIGHT")]
        public decimal? NetWeight { get; set; }

        /// <summary>
        /// 貨品總價。
        /// </summary>
        [Column("TOTAL_GOODS_PRICE")]
        public decimal? TotalGoodsPrice { get; set; }

        /// <summary>
        /// 寄件公司名稱。
        /// </summary>
        [Column("SEND_COP_NAME")]
        public string SendCopName { get; set; }

        /// <summary>
        /// 寄件人名稱。
        /// </summary>
        [Column("SENDER_NAME")]
        public string SenderName { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        [Column("RECEIVER_TEL")]
        public string ReceiverTel { get; set; }

        /// <summary>
        /// 收件人名稱。
        /// </summary>
        [Column("RECEIVER_NAME")]
        public string ReceiverName { get; set; }

        /// <summary>
        /// 收件人證號。
        /// </summary>
        [Column("RECEIVER_CERT_ID")]
        public string ReceiverCertId { get; set; }

        /// <summary>
        /// 運輸方式。
        /// </summary>
        [Column("TRANSPORT_TYPE")]
        public string TransportType { get; set; }

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
        /// 是否含稅。
        /// </summary>
        [Column("IS_INCLUDE_TAX")]
        public string IsIncludeTax { get; set; }

        /// <summary>
        /// 進口報關類型。
        /// </summary>
        [Column("IMPORT_CUSTOMS_TYPE")]
        public string ImportCustomsType { get; set; }

        /// <summary>
        /// 進口申報備註。
        /// </summary>
        [Column("IMPORT_DECLARE_REMARK")]
        public string ImportDeclareRemark { get; set; }

        /// <summary>
        /// 訂單類型。
        /// </summary>
        [Column("ORDER_TYPE")]
        public string OrderType { get; set; }

        /// <summary>
        /// 申報人姓名。
        /// </summary>
        [Column("DECLARANT_NAME")]
        public string DeclarantName { get; set; }

        /// <summary>
        /// 申報人手機。
        /// </summary>
        [Column("DECLARANT_MOBILE")]
        public string DeclarantMobile { get; set; }

        /// <summary>
        /// 申報人證件類型。
        /// </summary>
        [Column("DECLARANT_CERTIFICATE_TYPE")]
        public string DeclarantCertificateType { get; set; }

        /// <summary>
        /// 申報人證件號碼。
        /// </summary>
        [Column("DECLARANT_CERTIFICATE_NUM")]
        public string DeclarantCertificateNum { get; set; }

        /// <summary>
        /// 是否計入特定費用。
        /// </summary>
        [Column("IS_JY_ZR_FEE")]
        public string IsJyZrFee { get; set; }
    }
}