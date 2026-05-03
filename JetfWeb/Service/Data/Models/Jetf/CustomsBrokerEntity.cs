using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    [Table("CustomsBroker", Schema = "dbo")]
    public sealed class CustomsBrokerEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }
    }
}