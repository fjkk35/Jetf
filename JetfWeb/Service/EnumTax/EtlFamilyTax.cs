using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    /// <summary>
    /// 空運全家稅金
    /// </summary>
    public enum EtlFamilyTax
    {
        [Description("菜鳥空快全家稅金")]
        [Trans("15,15C,15P")]
        Cainiao,

        [Description("佐川空快全家稅金")]
        [Trans("117,117C")]
        Sagawa,
    }


    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class TransAttribute : Attribute
    {
        public string TransValue { get; }

        public TransAttribute(string transValue)
        {
            TransValue = transValue;
        }
    }

    public static class EnumExtensions
    {
        public static string GetTransValue(this Enum enumValue)
        {
            FieldInfo field = enumValue.GetType().GetField(enumValue.ToString());
            TransAttribute attribute = (TransAttribute)field.GetCustomAttribute(typeof(TransAttribute));
            return attribute?.TransValue;
        }
    }
}
