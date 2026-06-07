using System;
using System.ComponentModel;

namespace Service.EnumTax
{
    public enum ShenzhenTaxPayment
    {
        [Description("包稅")]
        XD,

        [Description("不包稅")]
        C,
    }
}
