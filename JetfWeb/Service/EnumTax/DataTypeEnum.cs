using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum DataTypeEnum
    {
        [Description("海運")]
        Sea,

        [Description("空運")]
        Etl,
    }
}
