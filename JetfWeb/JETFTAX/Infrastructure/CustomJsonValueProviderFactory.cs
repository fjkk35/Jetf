using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace JETFTAX.Infrastructure
{
    public class CustomJsonValueProviderFactory : ValueProviderFactory
    {
        public override IValueProvider GetValueProvider(ControllerContext controllerContext)
        {
            if (controllerContext == null)
                throw new ArgumentNullException(nameof(controllerContext));

            object deserializedObject = GetDeserializedObject(controllerContext);
            if (deserializedObject == null)
                return null;

            Dictionary<string, object> strs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            AddToBackingStore(strs, string.Empty, deserializedObject);

            return new DictionaryValueProvider<object>(strs, CultureInfo.CurrentCulture);
        }

        private static object GetDeserializedObject(ControllerContext controllerContext)
        {
            if (!controllerContext.HttpContext.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                return null;

            string bodyText;
            using (StreamReader streamReader = new StreamReader(controllerContext.HttpContext.Request.InputStream))
            {
                bodyText = streamReader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(bodyText))
                return null;

            JavaScriptSerializer serializer = new JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue
            };

            return serializer.DeserializeObject(bodyText);
        }

        private static void AddToBackingStore(Dictionary<string, object> backingStore, string prefix, object value)
        {
            IDictionary<string, object> dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                foreach (KeyValuePair<string, object> entry in dictionary)
                {
                    AddToBackingStore(backingStore, MakePropertyKey(prefix, entry.Key), entry.Value);
                }
                return;
            }

            IList<object> list = value as IList<object>;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    AddToBackingStore(backingStore, MakeArrayKey(prefix, i), list[i]);
                }
                return;
            }

            backingStore[prefix] = value;
        }

        private static string MakeArrayKey(string prefix, int index)
        {
            return prefix + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        }

        private static string MakePropertyKey(string prefix, string propertyName)
        {
            return string.IsNullOrEmpty(prefix) ? propertyName : prefix + "." + propertyName;
        }
    }
}
