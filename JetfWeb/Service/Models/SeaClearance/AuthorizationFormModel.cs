using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearance
{
    /// <summary>
    /// 授權表單 Model
    /// </summary>
    public class AuthorizationFormModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 表單名稱
        /// </summary>
        public string FormName { get; set; }
    }

    /// <summary>
    /// 海關通關授權表單 Model
    /// </summary>
    public class SeaClearanceAuthorizationFormModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 資料日期
        /// </summary>
        public DateTime DataDate { get; set; }

        /// <summary>
        /// 類型 (1=收到正本選單、2=寄文件選單)
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 海關通關明細 Id
        /// </summary>
        public int SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 明細列表
        /// </summary>
        public List<SeaClearanceAuthorizationFormDetailModel> Details { get; set; } = new List<SeaClearanceAuthorizationFormDetailModel>();
    }

    /// <summary>
    /// 海關通關授權表單明細 Model
    /// </summary>
    public class SeaClearanceAuthorizationFormDetailModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 海關通關授權表單 Id
        /// </summary>
        public int SeaClearanceAuthorizationFormId { get; set; }

        /// <summary>
        /// 授權表單 Id
        /// </summary>
        public int AuthorizationFormId { get; set; }

        /// <summary>
        /// 表單名稱
        /// </summary>
        public string FormName { get; set; }
    }

    /// <summary>
    /// 授權表單歷史記錄 Model
    /// </summary>
    public class AuthorizationFormHistoryModel
    {
        /// <summary>
        /// 日期
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 選單類型名稱
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 時間
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 建立人員
        /// </summary>
        public string CreateUser { get; set; }

        /// <summary>
        /// 表單名稱列表
        /// </summary>
        public string FormNames { get; set; }
    }
}