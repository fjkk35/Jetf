using Newtonsoft.Json;
using Org.BouncyCastle.Bcpg.Sig;
using Service.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaUnreceivedOrder
{

    public class SeaUnreceivedOrderModel
    {
        /// <summary>
        /// 航班主號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號碼
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 是否連線收單
        /// </summary>
        public bool IsReceiveOrder { get; set; }

        /// <summary>
        /// 客戶代號
        /// </summary>
        public string Cust_Code { get; set; }

        /// <summary>
        /// 捷利使用 => 如果有值要取代Consol_Name
        /// </summary>
        public string Sihno { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string Despatch_Name { get; set; }

        /// <summary>
        /// 倉儲
        /// </summary>
        public string ModifyBy { get; set; }

        /// <summary>
        /// 預計到港日
        /// </summary>
        public DateTime? Eta { get; set; }

        /// <summary>
        /// O:溢卸
        /// </summary>
        public string Merge_Over_Flag { get; set; }

        /// <summary>
        /// 進口人統一編號
        /// </summary>
        public string Importer_Id { get; set; }

        /// <summary>
        /// 進口人名稱
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 進口人電話
        /// </summary>
        public string Im_PhoneNo { get; set; }

        /// <summary>
        /// 進口人英文地址
        /// </summary>
        public string Im_Add { get; set; }

        /// <summary>
        /// 毛重
        /// </summary>
        public double Gw { get; set; }

        /// <summary>
        /// 件數
        /// </summary>
        public int Piece { get; set; }

        /// <summary>
        /// 貨物名稱
        /// </summary>
        public string Item_Name { get; set; }

        /// <summary>
        /// 單價金額
        /// </summary>
        public double Unit_Price { get; set; }

        /// <summary>
        /// 發票總金額
        /// </summary>
        public double Invoice_Amount { get; set; }

        /// <summary>
        /// 派件公司
        /// </summary>
        public string Trans_Name { get; set; }

        /// <summary>
        /// 配送單號
        /// </summary>
        public string Jetf_Serial { get; set; }

        /// <summary>
        /// 預委日期
        /// </summary>
        public string Customs_Approval_DateTime { get; set; }

        /// <summary>
        /// 拆櫃日
        /// </summary>
        public DateTime? UnboxingDataDate { get; set; }

        /// <summary>
        /// 現場有貨日期
        /// </summary>
        public DateTime? SiteCargoDataDate { get; set; }

        /// <summary>
        /// 短到日期
        /// </summary>
        public DateTime? ShortCargoDataDate { get; set; }

        /// <summary>
        /// 最後傳輸日
        /// 高雄郵聯(全旺) =>【預計到港日】+3
        /// TPCT(捷豐) =>【預計到港日】+7
        /// </summary>
        public DateTime? LastDataDate { get; set; }

        /// <summary>
        /// 格式化預委日期
        /// </summary>
        public string Format_Customs_Approval_DateTime =>
           DateTime.TryParseExact(Customs_Approval_DateTime, "yyyyMMddHHmmss",
               CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
               ? date.ToString("MM/dd")
               : "";

        public string LpNo { get; set; }

        public string Gb353RejReason { get; set; }

        public string UploadOpe { get; set; }

        /// <summary>
        /// 電商或集運商編號
        /// </summary>
        public string Consol_Code { get; set; }

        /// <summary>
        /// 貨物識別代碼
        /// </summary>
        public string Consol_Type { get; set; }

        /// <summary>
        /// 電商或集運商名稱
        /// </summary>
        public string Consol_Name { get; set; }

        /// <summary>
        /// 電商或集運商網址
        /// </summary>
        public string Consol_Url { get; set; }

        public List<Gb353RejReasonModel> Gb353RejReasonList
        {
            get
            {
                if (string.IsNullOrEmpty(Gb353RejReason))
                    return new List<Gb353RejReasonModel>();

                return JsonConvert.DeserializeObject<List<Gb353RejReasonModel>>(Gb353RejReason);
            }
        }

        /// <summary>
        /// 最新的Gb353代碼，時間相同都需要顯示
        /// </summary>
        public List<string> LastGb353RejReasonCode =>
             Gb353RejReasonList?
                 .GroupBy(x => x.IssueDateTime)
                 .OrderByDescending(x => x.Key)
                 .FirstOrDefault()?
                 .Select(x => x.RejReasonCode)
                 .ToList() ?? new List<string>();

       

        /// <summary>
        /// Gb353次數，時間相同只算一次
        /// </summary>
        public int Gb353Count =>
            Gb353RejReasonList?
                .GroupBy(x => x.IssueDateTime)
                .Count() ?? 0;

        /// <summary>
        /// 是否需更新預委-Excel上傳欄位
        /// </summary>
        public string IsUpdateApproval { get; set; }

        /// <summary>
        /// 是否需更新預委-程式判斷
        /// </summary>
        public bool IsUpdateApprovalNew { get; set; }

        /// <summary>
        /// 客服提供日期
        /// </summary>
        public DateTime? ServiceDate { get; set; }

        /// <summary>
        /// 正確姓名
        /// </summary>
        public string CorrectImporterName { get; set; }

        /// <summary>
        /// 正確ID
        /// </summary>
        public string CorrectImporterId { get; set; }

        /// <summary>
        /// 正確進口人電話
        /// </summary>
        public string CorrectImporterPhone { get; set; }

        /// <summary>
        /// 正確品名
        /// </summary>
        public string CorrectItemName { get; set; }

        /// <summary>
        /// 正確單票金額
        /// </summary>
        public string CorrectInvoiceAmount { get; set; }

        /// <summary>
        /// 今天客服狀態
        /// </summary>
        public string ServiceStatus { get; set; }

        /// <summary>
        /// 累積處置說明
        /// </summary>
        public string ProcessRemark { get; set; }


        public string Reply_Code { get; set; }

        /// <summary>
        /// 預委任-身分證Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 預委任-電話
        /// </summary>
        public string Tel { get; set; }

        /// <summary>
        /// 推撥與報單是否一致
        /// </summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// 是否重匯關貿
        /// </summary>
        public bool IsImport { get; set; }

    }

    public class Gb353RejReasonModel
    {
        public string RejReasonCode { get; set; }

        public string IssueDateTime { get; set; }
    }

    public enum ReasonCodeEnum
    {
        [Description("B15")]
        B15 = 1,
        [Description("A03")]
        A03 = 2,
        [Description("B6B")]
        B6B = 3,
        [Description("B6D")]
        B6D = 4,
        [Description("B6E")]
        B6E = 5,
        [Description("B6F")]
        B6F = 6,
        [Description("B6A")]
        B6A = 7
    }
}
