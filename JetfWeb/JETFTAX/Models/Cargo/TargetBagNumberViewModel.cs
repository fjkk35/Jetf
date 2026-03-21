using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.Cargo
{
    /// <summary>
    /// 貨況查詢-通關袋號
    /// </summary>
    public class TargetBagNumberViewModel
    {
        public List<TargetBagNumber> List { get; set; }
    }

    public class TargetBagNumber 
    {
        /// <summary>
        /// 通關袋號
        /// </summary>
        public string TargetCode { get; set; }

        /// <summary>
        /// 袋號
        /// </summary>
        public string SourceCode { get; set; }

        /// <summary>
        /// 進倉時間
        /// </summary>
        public string SignInTime { get; set; } = "";

        /// <summary>
        /// 出倉時間
        /// </summary>
        public string SignOutTime { get; set; } = "";
    }
}