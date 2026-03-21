using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearance
{
    /// <summary>
    /// 海運通關備註模型
    /// </summary>
    public class SeaClearanceRemarkModel
    {
        public int Id { get; set; }
        public int SeaClearanceDetailId { get; set; }
        public string Remark { get; set; }
        public string CrtUser { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
