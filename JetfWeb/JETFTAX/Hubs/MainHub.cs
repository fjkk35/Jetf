using Microsoft.AspNet.SignalR;
using Service.Services.ShipmentInboundProcess.Domain;

namespace JETFTAX.Hubs
{
    /// <summary>
    /// 全站共用的 SignalR Hub。
    /// </summary>
    public interface IMainHubClient
    {
        /// <summary>
        /// 通知貨件回倉處理列表中的單筆資料已更新。
        /// 前端對應事件名稱為 shipmentInboundProcessRowUpdated。
        /// </summary>
        /// <param name="model">最新的貨件回倉處理資料列。</param>
        void ShipmentInboundProcessRowUpdated(ShipmentInboundProcessModel model);
    }

    /// <summary>
    /// 全站共用的 SignalR Hub。
    /// </summary>
    public class MainHub : Hub<IMainHubClient>
    {
    }

    /// <summary>
    /// MainHub 的前端事件名稱集中定義。
    /// </summary>
    public static class MainHubClientEventNames
    {
        /// <summary>
        /// 貨件回倉處理列表單筆資料更新通知事件名稱。
        /// </summary>
        public const string ShipmentInboundProcessRowUpdated = "shipmentInboundProcessRowUpdated";
    }

    /// <summary>
    /// MainHub 的通知發送器。
    /// </summary>
    public static class MainHubNotifier
    {
        /// <summary>
        /// 廣播貨件回倉處理列表的單筆資料更新通知。
        /// </summary>
        /// <param name="model">最新的貨件回倉處理資料列。</param>
        public static void BroadcastRowUpdated(ShipmentInboundProcessModel model)
        {
            if (model == null)
            {
                return;
            }

            var context = GlobalHost.ConnectionManager.GetHubContext<MainHub, IMainHubClient>();
            context.Clients.All.ShipmentInboundProcessRowUpdated(model);
        }
    }
}