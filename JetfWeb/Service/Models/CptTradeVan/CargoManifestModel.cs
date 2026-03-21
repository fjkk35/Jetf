using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class CargoManifestModel
    {
        /// <summary>
        /// 主號
        /// </summary>
        public string MainNumber {get;set;}

        /// <summary>
        /// 船班
        /// </summary>
        //public string ShipName { get; set; }

        /// <summary>
        /// 船舶航次
        /// </summary>
        //public string VoyageFlightNo { get; set; }

        /// <summary>
        /// 貨櫃號碼
        /// </summary>
        public string ContainerNo { get; set; }

        /// <summary>
        /// 清關業者
        /// </summary>
        //public string ClearanceCustoms { get; set; }

        /// <summary>
        /// 海關通關號碼(海掛)
        /// </summary>
        public string VslRegNo { get; set; }

        /// <summary>
        /// 卸存地代碼
        /// </summary>
        public string StorWareCd { get; set; }

        /// <summary>
        /// 主號總票數
        /// </summary>
        public int? TotalCount { get; set; }

        /// <summary>
        /// 需預委票數
        /// </summary>
        public int? ResultCount { get; set; }

        /// <summary>
        /// 已按預委票數
        /// </summary>
        public int? ReplyCount { get; set; }

        /// <summary>
        /// 未按預票數
        /// </summary>
        public int? NotReplyCount { get; set; }

        /// <summary>
        /// 未按預委件數
        /// </summary>
        public int? NotPieceCount { get; set; }

        /// <summary>
        /// 目前銷艙比例
        /// </summary>
        public string ImCmRate { get; set; }

        /// <summary>
        /// 客戶
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// Gb330Msg
        /// </summary>
        //public string Gb330Msg { get; set; }

        /// <summary>
        /// Gb378Msg
        /// </summary>
        public string Gb378Msg { get; set; }

    }
}
