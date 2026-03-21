using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;

namespace JETFTAX.Extensions
{
    public static class ConvertToEnumExtensions
    {
        public static T ToEnum<T>(this object value) where T : struct, Enum
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null");
            }

            if (Enum.TryParse(value.ToString(), true, out T result))
            {
                return result;
            }
            else if (int.TryParse(value.ToString(), out int intValue) && Enum.IsDefined(typeof(T), intValue))
            {
                return (T)Enum.ToObject(typeof(T), intValue);
            }
            else
            {
                throw new ArgumentException($"Cannot convert {value} to {typeof(T).Name}");
            }
        }

        public static string ToDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field != null)
            {
                DescriptionAttribute attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
                if (attribute != null)
                {
                    return attribute.Description;
                }
            }
            return value.ToString();
        }
    }
}