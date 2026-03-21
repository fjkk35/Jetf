using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearance
{
    /// <summary>
    /// 步驟模型
    /// </summary>
    public class StepModel
    {
        public int Id { get; set; }
        public string StepName { get; set; }
        /// <summary>
        /// 是否多選
        /// </summary>
        public bool IsMultiple { get; set; }
        public int Sort { get; set; }
    }
}
