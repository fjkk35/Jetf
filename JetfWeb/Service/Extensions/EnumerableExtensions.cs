using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Service.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }
            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        public static int? GetSort(this Enum enumValue)
        {
            FieldInfo field = enumValue.GetType().GetField(enumValue.ToString());
            SortAttribute attribute = (SortAttribute)field.GetCustomAttribute(typeof(SortAttribute));
            return attribute?.Sort;
        }

        /// <summary>
        /// 將 Description 文字轉換為枚舉數字
        /// </summary>
        /// <typeparam name="TEnum">枚舉類型</typeparam>
        /// <param name="description">Description 文字</param>
        /// <returns>枚舉數字，若無對應則回傳 null</returns>
        public static int? ToEnumValueByDescription<TEnum>(this string description) where TEnum : Enum
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            var enumType = typeof(TEnum);
            
            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttribute != null && descriptionAttribute.Description == description)
                {
                    return Convert.ToInt32(field.GetValue(null));
                }
            }

            return null;
        }

        /// <summary>
        /// 取得枚舉所有有效的 Description 列表
        /// </summary>
        /// <typeparam name="TEnum">枚舉類型</typeparam>
        /// <returns>有效的 Description 集合</returns>
        public static HashSet<string> GetValidDescriptions<TEnum>() where TEnum : Enum
        {
            var validDescriptions = new HashSet<string>();
            var enumType = typeof(TEnum);
            
            foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttribute != null)
                {
                    validDescriptions.Add(descriptionAttribute.Description);
                }
            }

            return validDescriptions;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class SortAttribute : Attribute
    {
        public int Sort { get; }

        public SortAttribute(int sort)
        {
            Sort = sort;
        }
    }
}
