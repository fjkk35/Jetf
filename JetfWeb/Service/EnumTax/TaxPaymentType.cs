using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum TaxPaymentType
    {
        [Description("客戶")]
        P,

        [Description("代收")]
        Y,

        [Description("匯款")]
        D,
    }
}
