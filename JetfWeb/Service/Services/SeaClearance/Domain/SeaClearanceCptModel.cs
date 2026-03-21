using Service.Models.CptTradeVan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearance.Domain
{
    public class SeaClearanceCptModel
    {
        /// <summary>
        /// 放行附帶條件
        /// </summary>
        public string Gb301RelCondCd { get; set; }

        public List<CptGb301GridModel> Gb301GridModel { get; set; }

        public List<CptGb321GridModel> Gb321GridModel { get; set; }

        /// <summary>
        /// 是否有更新報單傳輸日、報單號碼
        /// </summary>
        public bool IsUpdate { get; set; }

        /// <summary>
        /// 更新的報單號碼
        /// </summary>
        public string UpdatedDeclNo { get; set; }

        /// <summary>
        /// 更新的報單傳輸日
        /// </summary>
        public DateTime? UpdatedProDateTime { get; set; }
    }

    public class CptGb301GridModel 
    {
        /// <summary>
        /// 處理日期時間
        /// </summary>
        public DateTime? ProDateTime { get; set; }
        /// <summary>
        /// 通關狀態代號
        /// </summary>
        public string ProcEventCodeStr { get; set; }
        /// <summary>
        /// 處理說明
        /// </summary>
        public object ProgDesc { get; set; }
    }

    public class CptGb321GridModel
    {
        /// <summary>
        /// 處理日期時間
        /// </summary>
        public DateTime? ProDateTime { get; set; }

        /// <summary>
        /// 處理狀況
        /// </summary>

        public string ProType { get; set; }
    }
}
