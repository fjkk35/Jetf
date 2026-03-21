using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearanceCreate
{
    public class SeaClearanceModel
    {
        public int Id { get; set; }

        public string FileName { get;set; }

        public string UploadOpe { get; set; }

        public DateTime CrtDateTime { get; set; }
    }
}
