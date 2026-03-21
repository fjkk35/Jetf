using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.AbnormalState
{
    /// <summary>
    /// 異常狀態模型
    /// </summary>
    public class AbnormalStateModel
    {
        public int Id { get; set; }
        public string AbnormalStateName { get; set; }
        public int? Sort { get; set; }
        public List<AbnormalStateDetailModel> AbnormalStateDetails { get; set; }
    }

    /// <summary>
    /// 異常狀態詳細模型
    /// </summary>
    public class AbnormalStateDetailModel
    {
        public int Id { get; set; }
        public int AbnormalStateId { get; set; }
        public string AbnormalStateDetailName { get; set; }
        public int? Sort { get; set; }
    }

    /// <summary>
    /// 異常狀態詳細請求模型
    /// </summary>
    public class AbnormalStateDetailRequestModel
    {
        public int Id { get; set; }
        public int AbnormalStateId { get; set; }
        public string AbnormalStateDetailName { get; set; }
        public int? Sort { get; set; }
    }

    /// <summary>
    /// 排序更新模型
    /// </summary>
    public class AbnormalStateSortUpdateModel
    {
        public int Id { get; set; }
        public int Sort { get; set; }
    }

    /// <summary>
    /// 異常狀態詳細排序更新模型
    /// </summary>
    public class AbnormalStateDetailSortUpdateModel
    {
        public int Id { get; set; }
        public int Sort { get; set; }
    }
}
