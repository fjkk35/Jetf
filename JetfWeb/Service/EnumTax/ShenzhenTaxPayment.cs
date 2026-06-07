using System;
using System.ComponentModel;

using Service.Extensions;

namespace Service.EnumTax
{
    public enum ShenzhenTaxPayment
    {
        [Description("包稅")]
        XD,

        [Description("不包稅")]
        C,
    }

    public static class ShenzhenTaxPaymentExtensions
    {
        public static bool TryParseCode(string value, out ShenzhenTaxPayment result)
        {
            return EnumerableExtensions.TryParseCode(value, out result);
        }

        public static ShenzhenTaxPayment? ParseNullableCode(string value)
        {
            return EnumerableExtensions.ParseNullableCode<ShenzhenTaxPayment>(value);
        }
    }
}
