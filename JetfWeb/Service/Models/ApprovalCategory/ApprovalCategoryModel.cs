using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.ApprovalCategory
{
    public class ApprovalCategoryModel
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public int Sort { get; set; }
    }
}