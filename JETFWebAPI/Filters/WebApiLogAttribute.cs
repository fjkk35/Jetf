using System;

namespace JETFWebAPI.Filters
{
    /// <summary>
    /// 標記需要記錄 NLog 日誌的 WebAPI Action
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class WebApiLogAttribute : Attribute
    {
        /// <summary>
        /// 是否記錄請求內容 (預設 false)
        /// </summary>
        public bool LogRequestBody { get; set; } = false;

        /// <summary>
        /// 是否記錄回應內容 (預設 true)
        /// </summary>
        public bool LogResponseBody { get; set; } = true;

        /// <summary>
        /// 最大記錄內容長度 (預設 2KB)
        /// </summary>
        public int MaxContentLength { get; set; } = 2048;

        /// <summary>
        /// 日誌描述
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 建構函式
        /// </summary>
        public WebApiLogAttribute()
        {
        }

        /// <summary>
        /// 建構函式 (帶描述)
        /// </summary>
        /// <param name="description">日誌描述</param>
        public WebApiLogAttribute(string description)
        {
            Description = description;
        }
    }
}