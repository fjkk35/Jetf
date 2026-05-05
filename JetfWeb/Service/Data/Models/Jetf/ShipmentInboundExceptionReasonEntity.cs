using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    [Table("ShipmentInboundExceptionReason", Schema = "dbo")]
    public sealed class ShipmentInboundExceptionReasonEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Reason")]
        public string Reason { get; set; }
    }
}
