using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Enum
{
    public enum WebhookEventType
    {
        /// <summary>
        /// 追蹤
        /// </summary>
        follow,

        /// <summary>
        /// 不追蹤
        /// </summary>
        unfollow,

        /// <summary>
        /// 訊息
        /// </summary>
        message,
    }
}