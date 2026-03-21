using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SearchCargo.Domain
{
    /// <summary>
    /// 通關分提單號明細
    /// </summary>
    public class CargoTargetTrackingNoModel
    {
        /// <summary>
        /// 通關分提單號
        /// </summary>
        public string TARGET_CODE { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string SOURCE_CODE { get; set; }

        /// <summary>
        /// 進倉時間
        /// </summary>
        public DateTime? SIGN_IN_TIME { get; set; }

        /// <summary>
        /// 出倉時間
        /// </summary>
        public DateTime? SIGN_OUT_TIME { get; set; }

        /// <summary>
        /// 格式化進倉時間
        /// </summary>
        public string Format_SIGN_IN_TIME { get; set; }

        /// <summary>
        /// 格式化出倉時間
        /// </summary>
        public string Format_SIGN_OUT_TIME { get; set; }
    }
}
