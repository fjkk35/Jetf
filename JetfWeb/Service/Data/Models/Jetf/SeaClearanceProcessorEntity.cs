using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 步驟處理人設定。
    /// </summary>
    [Table("SeaClearanceProcessor", Schema = "dbo")]
    public sealed class SeaClearanceProcessorEntity
    {
        /// <summary>
        /// 設定主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 步驟主鍵。
        /// </summary>
        [Column("StepId")]
        public int? StepId { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("Cust_Code")]
        public string CustCode { get; set; }

        /// <summary>
        /// X2 處理人員。
        /// </summary>
        [Column("X2")]
        public string X2 { get; set; }

        /// <summary>
        /// X3 處理人員。
        /// </summary>
        [Column("X3")]
        public string X3 { get; set; }

        /// <summary>
        /// G1 處理人員。
        /// </summary>
        [Column("G1")]
        public string G1 { get; set; }

        /// <summary>
        /// 移倉處理人員。
        /// </summary>
        [Column("MoveWarehouse")]
        public string MoveWarehouse { get; set; }

        /// <summary>
        /// 轉 G1 處理人員。
        /// </summary>
        [Column("TransferG1")]
        public string TransferG1 { get; set; }

        /// <summary>
        /// 轉倉處理人員。
        /// </summary>
        [Column("TransferWarehouse")]
        public string TransferWarehouse { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }
    }
}