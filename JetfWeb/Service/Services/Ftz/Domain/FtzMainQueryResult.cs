using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// 主號查詢
    /// </summary>
    public class FtzMainQueryResult
    {
        public List<MainRow> Rows { get; set; }

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


    public class MainRow
    {
        /// <summary>
        /// 分號
        /// </summary>
        public string Hwb { get; set; }

        /// <summary>
        /// 併袋
        /// </summary>
        public string ExpBagNo { get; set; }

        //public string Container { get; set; }

        //[JsonPropertyName("error_percent")]
        //public string ErrorPercent { get; set; }

        //[JsonPropertyName("merge_expbagNo")]
        //public string MergeExpbagNo { get; set; }

        //[JsonPropertyName("orgpc_wt")]
        //public string OrgpcWt { get; set; }

        //public string HwbCount { get; set; }

        //public string AirLine { get; set; }
        //public string BagFee { get; set; }
        //public string BagNo { get; set; }
        //public string BagPic { get; set; }
        //public string BagWeight { get; set; }

        //[JsonPropertyName("bond_loc")]
        //public string BondLoc { get; set; }

        //public string BoxNo { get; set; }
        //public string BoxNoExpressCName { get; set; }
        //public string BoxNoExpressId { get; set; }

        //public string C2Count { get; set; }
        //public string C3Count { get; set; }
        //public string C5Count { get; set; }

        //public string CancelGco { get; set; }
        //public string CancelRelease { get; set; }
        //public string ChargeBox { get; set; }
        //public string Chws { get; set; }

        //[JsonPropertyName("class")]
        //public string ClassName { get; set; }

        //public string ClearanceType { get; set; }
        //public string CloseMark { get; set; }
        //public string CloseMarkFee { get; set; }
        //public string CloseMarkName { get; set; }

        //[JsonPropertyName("cs_remark")]
        //public string CsRemark { get; set; }

        //public string DeclNo { get; set; }
        //public string DeclNo2 { get; set; }
        //public string DeclType { get; set; }
        //public string DutyNo { get; set; }
        //public string DutyPayment { get; set; }

        //public string Edi { get; set; }

        //[JsonPropertyName("error_percent_str")]
        //public string ErrorPercentStr { get; set; }

        //[JsonPropertyName("error_wt")]
        //public string ErrorWt { get; set; }

        //public string ExaminationNote { get; set; }

        //public string ExpAmount { get; set; }
        //public string ExpBag { get; set; }

        //public string ExpCount { get; set; }
        //public string ExpPic { get; set; }
        //public string ExpWeight { get; set; }

        //public string ExpressCname { get; set; }
        //public string ExpressId { get; set; }

        //public string Flag { get; set; }
        //public string FlightDate { get; set; }
        //public string FlightDest { get; set; }
        //public string FlightNo { get; set; }

        //public string GciDate1 { get; set; }
        //public string GciLogDate1 { get; set; }
        //public string GciPiece { get; set; }
        //public string GciUser { get; set; }
        //public string GciWeight { get; set; }

        //public string GcoDate1 { get; set; }
        //public string GcoLogDate1 { get; set; }
        //public string GcoPiece { get; set; }
        //public string GcoUser { get; set; }

        //public string HoldArea { get; set; }
        //public string HoldReason { get; set; }



        //public string Ie { get; set; }
        //public string IeType { get; set; }

        //public string ImpAmount { get; set; }
        //public string ImpCount { get; set; }
        //public string ImpPic { get; set; }
        //public string ImpWeight { get; set; }

        //public string Indicator { get; set; }
        //public string IsBag { get; set; }
        //public string Issuereason { get; set; }
        //public string ItemNo { get; set; }

        //public string Lastupdate { get; set; }
        //public string LockLogDate { get; set; }

        //[JsonPropertyName("manifest_so")]
        //public string ManifestSo { get; set; }

        //public string ManuClearanceType { get; set; }

        //public string Mwb { get; set; }
        //public string OrderBy { get; set; }

        //[JsonPropertyName("org_cs_remark")]
        //public string OrgCsRemark { get; set; }

        //[JsonPropertyName("org_flightDate")]
        //public string OrgFlightDate { get; set; }

        //[JsonPropertyName("orgpc_empty_wt")]
        //public string OrgpcEmptyWt { get; set; }

        //[JsonPropertyName("ori_clearancetype_dt")]
        //public string OriClearancetypeDt { get; set; }

        //public string OtherLogDate { get; set; }
        //public string Over24 { get; set; }

        //[JsonPropertyName("paper_qty")]
        //public string PaperQty { get; set; }

        //public string PaymentType { get; set; }

        //[JsonPropertyName("pc_empty_wt")]
        //public string PcEmptyWt { get; set; }

        //[JsonPropertyName("pc_wt")]
        //public string PcWt { get; set; }

        //public string Piece { get; set; }

        //public string PrintDuty { get; set; }
        //public string PrintSeq { get; set; }
        //public string PrintSn { get; set; }

        //[JsonPropertyName("print_dt")]
        //public string PrintDt { get; set; }

        //[JsonPropertyName("print_user")]
        //public string PrintUser { get; set; }

        //public string QueryDt1 { get; set; }
        //public string QueryDt2 { get; set; }
        //public string QuerySQL { get; set; }

        //[JsonPropertyName("rePrint_dt")]
        //public string RePrintDt { get; set; }

        //[JsonPropertyName("rePrint_user")]
        //public string RePrintUser { get; set; }

        //public string RealBag { get; set; }
        //public string RealBagCnt { get; set; }
        //public string RealBagPic { get; set; }
        //public string RealDutyPayment { get; set; }

        //public string RealGciPiece { get; set; }
        //public string RealGciWeight { get; set; }

        //public string RealPiece { get; set; }
        //public string RealTotBag { get; set; }
        //public string RealWeight { get; set; }

        //public string RealhwbCnt { get; set; }
        //public string RealhwbPic { get; set; }

        //public string ReleaseTime { get; set; }
        //public string Remarks { get; set; }
        //public string Rent { get; set; }

        //public string RptName { get; set; }
        //public string RptType { get; set; }

        //public string ServiceCenterCode { get; set; }

        //public string Sid { get; set; }
        //public string Svc { get; set; }

        //public string TotAmount { get; set; }
        //public string Ttype { get; set; }

        //public string UnlockLogDate { get; set; }
        //public string UpdUser { get; set; }
        //public string UpdateDeclNo { get; set; }

        //[JsonPropertyName("vessel_reg")]
        //public string VesselReg { get; set; }

        //public string Weight { get; set; }
        //public string WeightB { get; set; }

        //public string WorkArea { get; set; }
        //public string WorkArea2 { get; set; }
        //public string WorkArea3 { get; set; }
        //public string WorkArea4 { get; set; }

        //public string Ws { get; set; }
    }
}
