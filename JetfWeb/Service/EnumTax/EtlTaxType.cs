using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Service.EnumTax
{
    public enum EtlTaxType
    {
        /// <summary>
        /// 華儲
        /// </summary>
        [Description("TACT-華儲")]
        tact = 1,
        /// <summary>
        /// 遠雄
        /// </summary>
        [Description("FTZ-遠雄")]
        ftz = 2,
    }
}
