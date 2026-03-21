using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum CacheName
    {
        /// <summary>
        /// 取得所有步驟
        /// </summary>
        GetAllSteps,

        /// <summary>
        /// 取得所有簽審類別
        /// </summary>
        GetApprovalCategory,

        /// <summary>
        /// 取得所有授權表單
        /// </summary>
        GetAuthorizationForm,

        /// <summary>
        /// 異常狀態
        /// </summary>
        GetAllAbnormalStates,

        /// <summary>
        /// 關貿GB301、GB321資料查詢
        /// </summary>
        SeaClearanceCptData,
    }
}
