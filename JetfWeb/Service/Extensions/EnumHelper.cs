using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Service.Extensions
{
    public static class EnumHelper
    {
        /// <summary>
        /// 將 Enum 轉換成 List<SelectListItem>
        /// </summary>
        /// <typeparam name="TEnum">Enum 類型</typeparam>
        /// <returns>List<SelectListItem></returns>
        public static List<SelectListItem> ToSelectList<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                       .Cast<TEnum>()
                       .Select(e => new SelectListItem
                       {
                           Text = e.ToDescription(),
                           Value = e.ToString()
                       })
                       .ToList();
        }
    }
}
