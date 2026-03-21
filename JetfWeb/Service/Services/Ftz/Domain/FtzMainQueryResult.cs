using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// 主號查詢
    /// </summary>
    public class FtzMainQueryResult
    {
        public UserData userdata { get; set; }
    }

    public class UserData
    {
        /// <summary>
        /// 併袋進倉重量
        /// </summary>
        public string expBagGciWt { get; set; }

        /// <summary>
        /// 分號
        /// </summary>
        public string hwbCount { get; set; }

        /// <summary>
        /// 進倉重量
        /// </summary>
        public string gciWeight { get; set; }

        /// <summary>
        /// 進倉重量
        /// </summary>
        public string hwbGciWt { get; set; }

        /// <summary>
        /// 出倉併袋數量：0
        /// </summary>
        public string expBagGcoCount { get; set; }

        /// <summary>
        /// 分號：2276筆
        /// </summary>
        public int count { get; set; }

        /// <summary>
        /// 申報
        /// </summary>
        public string hwbPiece { get; set; }

        /// <summary>
        /// 總重量
        /// </summary>
        public string weight { get; set; }

        /// <summary>
        /// 進倉併袋數量
        /// </summary>
        public string expBagGciCount { get; set; }

        /// <summary>
        /// 總袋數
        /// </summary>
        public string totBag { get; set; }

        /// <summary>
        /// 併袋
        /// </summary>
        public string expBagCount { get; set; }

        /// <summary>
        /// 出倉併袋件數
        /// </summary>
        public string expBagGcoPiece { get; set; }

        /// <summary>
        /// 出倉
        /// </summary>
        public string hwbGcoPiece { get; set; }

        /// <summary>
        /// 進倉併袋件數
        /// </summary>
        public string expBagGciPiece { get; set; }

        /// <summary>
        /// 進倉
        /// </summary>
        public string hwbGciPiece { get; set; }

        /// <summary>
        /// 併袋申報件數
        /// </summary>
        public string expBagHwbCount { get; set; }

        /// <summary>
        /// 併袋件數
        /// </summary>
        public string expBagPiece { get; set; }
    }
}
