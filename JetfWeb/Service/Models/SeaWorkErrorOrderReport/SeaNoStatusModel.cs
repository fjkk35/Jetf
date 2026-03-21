using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaWorkErrorOrderReport
{
    public class SeaNoStatusModel
    {
        public string MainNumber { get; set; }

        public string BagNumber { get; set; }

        public string Post_Entry { get; set; }

        /// <summary>
        /// 是否後段報關
        /// </summary>
        public string IsPostEntry =>
            string.IsNullOrEmpty(Post_Entry) ? "" : "V";
    }
}
