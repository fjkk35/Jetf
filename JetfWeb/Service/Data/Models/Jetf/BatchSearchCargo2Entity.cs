using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    [Table("BatchSearchCargo2", Schema = "dbo")]
    public sealed class BatchSearchCargo2Entity
    {
        [Key]
        [Column("TrackingNo", Order = 0)]
        public string TrackingNo { get; set; }

        [Key]
        [Column("Upload_Time", Order = 1)]
        public DateTime UploadTime { get; set; }

        [Key]
        [Column("Upload_Ope", Order = 2)]
        public string UploadOpe { get; set; }
    }
}