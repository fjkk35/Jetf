using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Service.Services
{
    public class SelectListService
    {
        /// <summary>
        /// 取得空運稅金種類
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SelectListItem> GetEtlTaxTypeList()
        {
            var etlTaxTypeList = Enum.GetValues(typeof(EtlTaxType)).Cast<EtlTaxType>()
                                .Select(item => new SelectListItem
                                {
                                    Value = item.ToString(),
                                    Text = GetDescription(item)
                                });
            return etlTaxTypeList;
        }

        /// <summary>
        /// 取得華儲查詢-華儲Url
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SelectListItem> GetTactReptileUrlList()
        {
            var list = Enum.GetValues(typeof(TactReptileUrl)).Cast<TactReptileUrl>()
                                .Select(item => new SelectListItem
                                {
                                    Value = item.ToString(),
                                    Text = GetDescription(item)
                                });
            return list;
        }

        /// <summary>
        /// 取得遠雄查詢-遠雄Url
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SelectListItem> GetFtzReptileUrlList()
        {
            var list = Enum.GetValues(typeof(FtzReptileUrl)).Cast<FtzReptileUrl>()
                                .Select(item => new SelectListItem
                                {
                                    Value = item.ToString(),
                                    Text = GetDescription(item)
                                });
            return list;
        }

        /// <summary>
        /// 取得Enum名稱
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }
}
