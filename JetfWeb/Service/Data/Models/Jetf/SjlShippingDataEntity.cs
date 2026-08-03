using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 捷利托運資料。
    /// </summary>
    [Table("SjlShippingData", Schema = "dbo")]
    public sealed class SjlShippingDataEntity
    {
        /// <summary>
        /// 主鍵識別碼。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// Jetf 服務序號。
        /// </summary>
        [Column("JetfSerial")]
        public string JetfSerial { get; set; }

        /// <summary>
        /// 收件人。
        /// </summary>
        [Column("Importer")]
        public string Importer { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        [Column("ImporterPhone")]
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 收件地址。
        /// </summary>
        [Column("ImporterAddr")]
        public string ImporterAddr { get; set; }

        /// <summary>
        /// 重量。
        /// </summary>
        [Column("Gw")]
        public decimal? Gw { get; set; }

        /// <summary>
        /// 代收金額。
        /// </summary>
        [Column("Cod")]
        public int? Cod { get; set; }
    }
}
