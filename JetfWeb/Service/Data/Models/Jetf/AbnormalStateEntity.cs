using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    [Table("AbnormalState", Schema = "dbo")]
    public sealed class AbnormalStateEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("AbnormalStateName")]
        public string AbnormalStateName { get; set; }
    }
}