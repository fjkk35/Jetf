using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 主單資訊。
    /// </summary>
    [Table("MAINORDERINFO", Schema = "dbo")]
    public sealed class MainOrderInfoEntity
    {
        /// <summary>
        /// 資料主鍵。
        /// </summary>
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>
        /// 派送來源代碼。
        /// </summary>
        [Column("DELIVERYFROM")]
        public string DeliveryFrom { get; set; }

        /// <summary>
        /// 派送日期。
        /// </summary>
        [Column("DELIVERYDATE")]
        public DateTime? DeliveryDate { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAINNUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        [Column("TAXPAYER")]
        public string TaxPayer { get; set; }

        /// <summary>
        /// 航班號碼。
        /// </summary>
        [Column("FLIGHTNUMBER")]
        public string FlightNumber { get; set; }

        /// <summary>
        /// 港口代碼。
        /// </summary>
        [Column("PORTCODE")]
        public string PortCode { get; set; }

        /// <summary>
        /// 貨櫃號。
        /// </summary>
        [Column("CONTAINERNO")]
        public string ContainerNo { get; set; }

        /// <summary>
        /// 承辦人員。
        /// </summary>
        [Column("RESPERSON")]
        public string ResPerson { get; set; }

        /// <summary>
        /// 訂單狀態。
        /// </summary>
        [Column("ORDERSTATUS")]
        public string OrderStatus { get; set; }

        /// <summary>
        /// 倉庫代碼。
        /// </summary>
        [Column("WAREHOUSE")]
        public int? Warehouse { get; set; }

        /// <summary>
        /// 是否為正式資料。
        /// </summary>
        [Column("IS_REAL")]
        public string IsReal { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CREATE_TIME")]
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 進口日期。
        /// </summary>
        [Column("IMPORT_DATE")]
        public DateTime? ImportDate { get; set; }

        /// <summary>
        /// 執行時間。
        /// </summary>
        [Column("EXECUTE_TIME")]
        public DateTime? ExecuteTime { get; set; }
    }
}