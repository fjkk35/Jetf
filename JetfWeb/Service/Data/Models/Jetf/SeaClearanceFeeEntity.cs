using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    [Table("SeaClearanceFee", Schema = "dbo")]
    public sealed class SeaClearanceFeeEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("CustCode")]
        public string CustCode { get; set; }

        [Column("G1Fee")]
        public int G1Fee { get; set; }

        [Column("MoveWarehouseFee")]
        public int MoveWarehouseFee { get; set; }

        [Column("TransferG1Fee")]
        public int TransferG1Fee { get; set; }

        [Column("TransferWarehouseFee")]
        public int TransferWarehouseFee { get; set; }

        [Column("X2Fee")]
        public int X2Fee { get; set; }
    }
}