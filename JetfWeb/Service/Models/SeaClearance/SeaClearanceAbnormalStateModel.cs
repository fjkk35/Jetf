using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearance
{
    /// <summary>
    /// 海運通關異常狀態模型
    /// </summary>
    public class SeaClearanceAbnormalStateModel
    {
        public int Id { get; set; }
        public int SeaClearanceDetailId { get; set; }
        public int AbnormalStateId { get; set; }
        public string AbnormalStateName { get; set; }
        public DateTime DataDate { get; set; }
        public string CrtUser { get; set; }
        public DateTime CreateTime { get; set; }
        public List<SeaClearanceAbnormalStateDetailItemModel> AbnormalStateDetails { get; set; }
    }

    /// <summary>
    /// 海運通關異常狀態詳細項目模型
    /// </summary>
    public class SeaClearanceAbnormalStateDetailItemModel
    {
        public int Id { get; set; }
        public int AbnormalStateDetailId { get; set; }
        public string AbnormalStateDetailName { get; set; }
    }
}
