using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    [Table("CustomsBrokerage", Schema = "dbo")]
    public sealed class CustomsBrokerageEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }
    }
}