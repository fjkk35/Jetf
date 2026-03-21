using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.Step
{
    /// <summary>
    /// 步驟模型
    /// </summary>
    public class StepModel
    {
        public int Id { get; set; }
        public string StepName { get; set; }
        /// <summary>
        /// 是否可以多選
        /// </summary>
        public bool IsMultiple { get; set; }
        public int? Sort { get; set; }
        public List<StepDetailModel> StepDetails { get; set; }
    }

    /// <summary>
    /// 步驟詳細模型
    /// </summary>
    public class StepDetailModel
    {
        public int Id { get; set; }
        public int StepId { get; set; }
        public string StepDetailName { get; set; }
        public int? Sort { get; set; }
    }

    /// <summary>
    /// 步驟詳細請求模型
    /// </summary>
    public class StepDetailRequestModel
    {
        public int Id { get; set; }
        public int StepId { get; set; }
        public string StepDetailName { get; set; }
        public int? Sort { get; set; }
    }

    /// <summary>
    /// 排序更新模型
    /// </summary>
    public class StepSortUpdateModel
    {
        public int Id { get; set; }
        public int Sort { get; set; }
    }

    /// <summary>
    /// 步驟詳細排序更新模型
    /// </summary>
    public class StepDetailSortUpdateModel
    {
        public int Id { get; set; }
        public int Sort { get; set; }
    }
}