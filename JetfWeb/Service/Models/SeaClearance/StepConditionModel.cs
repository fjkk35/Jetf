using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearance
{
    /// <summary>
    /// 步驟跳轉條件模型
    /// </summary>
    public class StepConditionModel
    {
        public int Id { get; set; }
        public int StepId { get; set; }
        public int? RequiredStepDetailId { get; set; }
        public int NextStepId { get; set; }
        public int ConditionType { get; set; }
        
        /// <summary>
        /// 下一步驟名稱
        /// </summary>
        public string NextStepName { get; set; }
        
        /// <summary>
        /// 必要條件步驟詳細名稱
        /// </summary>
        public string RequiredStepDetailName { get; set; }
    }
    
    /// <summary>
    /// 可用步驟模型（包含步驟及其可用條件）
    /// </summary>
    public class AvailableStepModel
    {
        public int StepId { get; set; }
        public string StepName { get; set; }
        public int Sort { get; set; }
        public bool IsAvailable { get; set; }
        public string Reason { get; set; }
    }
}
