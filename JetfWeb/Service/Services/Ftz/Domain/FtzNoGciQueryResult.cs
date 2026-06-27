using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    public class FtzNoGciQueryResult
    {
        //public UserData userdata { get; set; }
        public int total { get; set; }
        public int records { get; set; }
        public string page { get; set; }
        public List<Row> rows { get; set; }
    }

    /// <summary>
    /// 申報未進倉明細
    /// </summary>
    public class Row
    {
        /// <summary>提單號碼(主號)</summary>
        public string mwb { get; set; }

        /// <summary>分號</summary>
        public string hwb { get; set; }

        /// <summary>報單號碼</summary>
        public string declNo { get; set; }

        /// <summary>袋號</summary>
        public string expBagNo { get; set; }

        /// <summary>報關類別</summary>
        public string declType { get; set; }

        /// <summary>進倉</summary>
        public string gciPiece { get; set; }

        /// <summary>出倉</summary>
        public string gcoPiece { get; set; }
        /// <summary>
        /// 申報
        /// </summary>
        public string piece { get; set; }

        /// <summary>備註</summary>
        public string remarks { get; set; }

        /// <summary>一分號多件</summary>
        public string realTotBag { get; set; }

        /// <summary>一分號多件數</summary>
        public int realTotBagCount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(realTotBag))
                    return 0;

                return realTotBag.Split(',').Length;
            }
        }

        // 其他欄位保留以便完整接收 JSON
        public string clearanceType { get; set; }
        public string flightNo { get; set; }
        public string releaseTime { get; set; }
        public string sid { get; set; }
        public string dutyPayment { get; set; }
        public string serviceCenterCode { get; set; }
        public string weight { get; set; }
        public string impAmount { get; set; }
        public string expAmount { get; set; }
        public string realWeight { get; set; }
        public string gciUser { get; set; }
        public string gcoUser { get; set; }
        public string className { get; set; }
        public string flightDate { get; set; }
        public string flightDest { get; set; }
        public string airLine { get; set; }
        public string expPic { get; set; }
        public string impPic { get; set; }
        public string bagPic { get; set; }
        public string realBagPic { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string TransName { get; set; }
    }

    //public class UserData
    //{
    //    public string hwbCount { get; set; }
    //    public int gciPiece { get; set; }
    //    public string expBagGcoCount { get; set; }
    //    public int count { get; set; }
    //    public string hwbPiece { get; set; }
    //    public string expBagGciCount { get; set; }
    //    public string totBag { get; set; }
    //    public string expBagCount { get; set; }
    //    public int hwb { get; set; }
    //    public string expBagGcoPiece { get; set; }
    //    public string hwbGcoPiece { get; set; }
    //    public string expBagGciPiece { get; set; }
    //    public string hwbGciPiece { get; set; }
    //    public int piece { get; set; }
    //    public string expBagHwbCount { get; set; }
    //    public int mwb { get; set; }
    //    public string expBagPiece { get; set; }
    //    public int gcoPiece { get; set; }
    //}
}
