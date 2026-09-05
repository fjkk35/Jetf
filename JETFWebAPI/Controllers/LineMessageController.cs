using JETFWebAPI.Models.Jetf;
using JETFWebAPI.Models.LineMessage;
using JETFWebAPI.Services;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace JETFWebAPI.Controllers
{
    public class LineMessageController : ApiController
    {
        Logger logger = NLog.LogManager.GetCurrentClassLogger();

        LineMessageService _lineMessageService = new LineMessageService();

        public IHttpActionResult Webhook([FromBody] LineWebhookEvent request)
        {
            logger.Debug(JsonConvert.SerializeObject(request));

            //_lineMessageService.UpdateLineUserProfile();

            _lineMessageService.LineMessageWebhookAsync(request);

            return Ok();
        }
    }
}
