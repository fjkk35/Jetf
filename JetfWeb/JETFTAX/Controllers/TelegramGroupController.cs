using JETFTAX.Models.TelegramGroup;
using Service.Services.TelegramGroup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class TelegramGroupController : Controller
    {
        private readonly TelegramGroupService _telegramGroupService;

        public TelegramGroupController(TelegramGroupService telegramGroupService)
        {
            _telegramGroupService = telegramGroupService;
        }

        // GET: TelegramGroup
        public ActionResult Index()
        {
            var vm = new TelegramGroupViewModel()
            {
               List = _telegramGroupService.GetTelegramGroup()
            }; 
            return View(vm);
        }

        public async Task<ActionResult> SendTextMessageAsync(string chatId)
        {
            var telegramResponse = await _telegramGroupService.SendTextMessageAsync(chatId, "測試");
            return Json(telegramResponse, JsonRequestBehavior.AllowGet);
        }
    }
}