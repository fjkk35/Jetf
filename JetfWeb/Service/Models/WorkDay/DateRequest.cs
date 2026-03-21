using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.WorkDay
{
    public class DateRequest
    {
        public string Date { get; set; }

        public DateType DateType { get; set; }
    }
}
