using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum ErrorOrderType
    {
        /// <summary>
        /// 需要預先委任
        /// </summary>
        [Description("B6F-需要預先委任")]
        B6F,

        /// <summary>
        /// 未註冊EZway
        /// </summary>
        [Description("B6E-未註冊EZway")]
        B6E,

        /// <summary>
        /// 申報人姓名與身分證號不符，或是ID空白，姓名與電話不符
        /// </summary>
        [Description("B6D-申報人姓名與身分證號不符，或是ID空白，姓名與電話不符")]
        B6D,

        /// <summary>
        /// 在裝貨港前兩碼為CN、HK、MO(澳門)時，須填列中文姓名
        /// </summary>
        [Description("在裝貨港前兩碼為CN、HK、MO(澳門)時，須填列中文姓名")]
        CNHKMO,

    }
}
