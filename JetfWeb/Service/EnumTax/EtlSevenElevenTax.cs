using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    /// <summary>
    /// 空運7-11稅金
    /// </summary>
    public enum EtlSevenElevenTax
    {
        [Description("佐川7-11空運稅金")]
        [Trans("116,116C")]
        Sagawa,

        [Description("菜鳥7-11空運稅金")]
        [Trans("17,17C")]
        Cainiao,
    }

}
