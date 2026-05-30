using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    [Table("Cargo_Sign_Receipt", Schema = "dbo")]
    public sealed class CargoSignReceiptEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("DataDate")]
        [StringLength(8)]
        public string DataDate { get; set; }

        [Column("Jetf_Serial")]
        [StringLength(20)]
        public string JetfSerial { get; set; }

        [Column("FilePath")]
        [StringLength(200)]
        public string FilePath { get; set; }

        [Column("FileName")]
        [StringLength(100)]
        public string FileName { get; set; }

        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }
    }
}
