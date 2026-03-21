using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.Importer
{
    public class ImporterResponse
    {
        /// <summary>
        /// 手機或身分證Id
        /// </summary>
        public string PhoneOrId { get; set; }

        /// <summary>
        /// 申報人
        /// </summary>
        public string E_Importer { get; set; }
    }
}
