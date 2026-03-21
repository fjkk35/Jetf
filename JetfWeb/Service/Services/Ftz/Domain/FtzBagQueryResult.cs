using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    public class FtzBagQueryResult
    {
        public int total { get; set; }
        public int records { get; set; }
        public string page { get; set; }
        public List<RowItem> rows { get; set; }
    }

    public class RowItem
    {
        /// <summary>
        /// 袋號
        /// </summary>
        public string bagNo { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string declNo { get; set; }

        /// <summary>
        /// 主號 (MAWB)
        /// </summary>
        public string mwb { get; set; }

        /// <summary>
        /// 分號 (HAWB)
        /// </summary>
        public string hwb { get; set; }

        /// <summary>
        /// 航班
        /// </summary>
        public string flightNo { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        public string gciWeight { get; set; }

        /// <summary>
        /// 申報
        /// </summary>
        public string piece { get; set; }

        /// <summary>
        /// 進倉
        /// </summary>
        public string gciPiece { get; set; }

        /// <summary>
        /// 出倉
        /// </summary>
        public string gcoPiece { get; set; }

        /// <summary>
        /// 通關方式
        /// </summary>
        public string clearanceType { get; set; }

        /// <summary>
        /// 驗貨窗口
        /// </summary>
        public string examinationNote { get; set; }

        /// <summary>
        /// 卸存地
        /// </summary>
        public string workArea { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string remarks { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string message { get; set; }

        // 以下為其餘欄位（自動生成保留）
        public string container { get; set; }
        public string error_percent { get; set; }
        public string merge_expbagNo { get; set; }
        public string orgpc_wt { get; set; }
        public string hwbCount { get; set; }
        public string expPic { get; set; }
        public string gcoDate1_ { get; set; }
        public string totAmount { get; set; }
        public string printSeq { get; set; }
        public string rent { get; set; }
        public string realhwbCnt { get; set; }
        public string over24 { get; set; }
        public string lockLogDate { get; set; }
        public string orgpc_empty_wt { get; set; }
        public string print_dt { get; set; }
        public string ie { get; set; }
        public string org_cs_remark { get; set; }
        public string c2Count { get; set; }
        public string realGciWeight { get; set; }
        public string flightDate { get; set; }
        public string flightDest { get; set; }
        public string realhwbPic { get; set; }
        public string realDutyPayment { get; set; }
        public string declNo2 { get; set; }
        public string boxNo { get; set; }
        public string cs_remark { get; set; }
        public string dutyNo { get; set; }
        public string expBagNo { get; set; }
        public string impPic { get; set; }
        public string rptType { get; set; }
        public string indicator { get; set; }
        public string realPiece { get; set; }
        public string unlockLogDate { get; set; }
        public string isBag { get; set; }
        public string ori_clearancetype_dt { get; set; }
        public string boxNoExpressCName { get; set; }
        public string expBag { get; set; }
        public string impWeight { get; set; }
        public string expWeight { get; set; }
        public string itemNo { get; set; }
        public string gciDate1_2 { get; set; }
        public string org_flightDate { get; set; }
        public string airLine { get; set; }
        public string rptName { get; set; }
        public string impAmount { get; set; }
        public string querySQL { get; set; }
        public string ieType { get; set; }
        public string updUser { get; set; }
        public string boxNoExpressId { get; set; }
        public string expressCname { get; set; }
        public string weightB { get; set; }
        public string bagFee { get; set; }
        public string bagPic { get; set; }
        public string c5Count { get; set; }
        public string gcoUser { get; set; }
        public string pc_empty_wt { get; set; }
        public string edi { get; set; }
        public string bond_loc { get; set; }
        public string chws { get; set; }
        public string otherLogDate { get; set; }
        public string expCount { get; set; }
        public string ttype { get; set; }
        public string expressId { get; set; }
        public string error_wt { get; set; }
        public string flag { get; set; }
        public string orderBy { get; set; }
        public string paper_qty { get; set; }
        public string c3Count { get; set; }
        public string error_percent_str { get; set; }
        public string paymentType { get; set; }
        public string gciLogDate1 { get; set; }
        public string print_user { get; set; }
        public string closeMarkName { get; set; }
        public string cancelGco { get; set; }
        public string svc { get; set; }
        public string realBagCnt { get; set; }
        public string manuClearanceType { get; set; }
        public string realhwbCnt2 { get; set; }
        public string printSn { get; set; }
        public string realGciPiece { get; set; }
        public string ws { get; set; }
        public string @class { get; set; }
        public string closeMark { get; set; }
        public string rePrint_user { get; set; }
        public string gcoLogDate1 { get; set; }
        public string realBag { get; set; }
        public string chargeBox { get; set; }
        public string realWeight { get; set; }
        public string workArea2 { get; set; }
        public string realBagPic { get; set; }
        public string workArea3 { get; set; }
        public string vessel_reg { get; set; }
        public string workArea4 { get; set; }
        public string pc_wt { get; set; }
        public string cancelRelease { get; set; }
        public string serviceCenterCode { get; set; }
        public string gciUser { get; set; }
        public string holdReason { get; set; }
        public string manifest_so { get; set; }
        public string dutyPayment { get; set; }
        public string bagWeight { get; set; }
        public string realTotBag { get; set; }
        public string printDuty { get; set; }
        public string releaseTime { get; set; }
        public string impCount { get; set; }
        public string queryDt1 { get; set; }
        public string queryDt2 { get; set; }
        public string sid { get; set; }
        public string declType { get; set; }
        public string issuereason { get; set; }
        public string holdArea { get; set; }
        public string realGciWeight2 { get; set; }
        public string updateDeclNo { get; set; }
        public string closeMarkFee { get; set; }
        public string lastupdate { get; set; }
        public string rePrint_dt { get; set; }
    }
}
