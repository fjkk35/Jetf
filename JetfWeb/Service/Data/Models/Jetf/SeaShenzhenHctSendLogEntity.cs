using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 新遞深圳託運資料送至 HCT 的傳送紀錄。
    /// </summary>
    [Table("SeaShenzhenHctSendLog", Schema = "dbo")]
    public class SeaShenzhenHctSendLogEntity
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("SeaShenzhenOriginalId")]
        public int SeaShenzhenOriginalId { get; set; }

        [Column("JetfSerial")]
        [StringLength(50)]
        public string JetfSerial { get; set; }

        [Column("Success")]
        [StringLength(1)]
        public string Success { get; set; }

        [Column("ErrMsg")]
        [StringLength(100)]
        public string ErrMsg { get; set; }

        [Column("RequestJson")]
        public string RequestJson { get; set; }

        [Column("ResponseJson")]
        public string ResponseJson { get; set; }

        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }
    }
}