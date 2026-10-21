using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TelegramPortalApi.Models;
using TelegramPortalApi.Service;

namespace TelegramPortalApi.Controllers
{
    public class TelegramController : ApiController
    {
        private readonly TelegramService _telegramService;

        public TelegramController()
        {
            _telegramService = new TelegramService();
        }

        public async Task<IHttpActionResult> SendMessage(TelegramRequest request)
        { 
            var result = await _telegramService.SendMessageAsync(request);

            var response = new TelegramResponse
            {
                Status = result.Ok ? 200 : 0,
                Message = result.Description
            };

            return Ok(response);
        }
    }
}
