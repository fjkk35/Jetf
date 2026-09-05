using JETFWebAPI.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.LineMessage
{
    public class LineWebhookEvent
    {
        /// <summary>
        /// 接收事件的目標
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// 事件列表
        /// </summary>
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        /// <summary>
        /// 事件類型
        /// </summary>
        public WebhookEventType Type { get; set; }

        /// <summary>
        /// 關注事件的詳細資訊
        /// </summary>
        public Follow Follow { get; set; }

        /// <summary>
        /// Webhook 事件 ID
        /// </summary>
        public string WebhookEventId { get; set; }

        /// <summary>
        /// 傳遞上下文
        /// </summary>
        public DeliveryContext DeliveryContext { get; set; }

        /// <summary>
        /// 事件的時間戳記
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 事件來源
        /// </summary>
        public Source Source { get; set; }

        /// <summary>
        /// 回覆的 Token
        /// </summary>
        public string ReplyToken { get; set; }

        /// <summary>
        /// 事件模式
        /// </summary>
        public string Mode { get; set; }
    }

    public class Follow
    {
        /// <summary>
        /// 是否追蹤
        /// </summary>
        public bool IsUnblocked { get; set; }
    }

    public class DeliveryContext
    {
        /// <summary>
        /// 是否重新傳遞
        /// </summary>
        public bool IsRedelivery { get; set; }
    }

    public class Source
    {
        /// <summary>
        /// 來源類型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 使用者 ID
        /// </summary>
        public string UserId { get; set; }
    }

}