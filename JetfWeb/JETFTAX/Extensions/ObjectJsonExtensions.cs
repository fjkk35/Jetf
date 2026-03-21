using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFTAX.Extensions
{
    public static class ObjectJsonExtensions
    {
		private static readonly IList<JsonConverter> _defaultConverters = new List<JsonConverter>
		{
			new StringEnumConverter(),
			new IsoDateTimeConverter()
		};

		public static string ToJson(this object obj)
		{
			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
			jsonSerializerSettings.Converters = ObjectJsonExtensions._defaultConverters;
			return JsonConvert.SerializeObject(obj, null, jsonSerializerSettings);
		}

		public static IHtmlString ToJsonRaw(this object obj)
		{
			return new HtmlString(obj.ToJson());
		}

		public static TObject JsonToObject<TObject>(this string jsonStr)
		{
			if (jsonStr == null)
			{
				return default(TObject);
			}
			return JsonConvert.DeserializeObject<TObject>(jsonStr);
		}
	}
}