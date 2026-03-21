using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 日誌管理控制器
    /// </summary>
    [LoginFilter]
    public class LogViewerController : Controller
    {
        private readonly string logBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        /// <summary>
        /// 日誌檢視首頁
        /// </summary>
        public ActionResult Index()
        {
            var users = GetAllLogUsers();
            ViewBag.Users = users;
            return View();
        }

        /// <summary>
        /// 取得指定使用者的日誌列表
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="date">日期 (可選)</param>
        /// <returns></returns>
        public ActionResult GetUserLogs(string userId, string date = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "使用者ID不能為空" }, JsonRequestBehavior.AllowGet);
                }

                var userLogPath = Path.Combine(logBasePath, "UserActions", userId);
                
                if (!Directory.Exists(userLogPath))
                {
                    return Json(new { success = false, message = $"使用者 {userId} 沒有日誌記錄" }, JsonRequestBehavior.AllowGet);
                }

                var logFiles = Directory.GetFiles(userLogPath, "*.log")
                    .Select(f => new
                    {
                        FileName = Path.GetFileName(f),
                        Date = Path.GetFileNameWithoutExtension(f),
                        Size = FormatFileSize(new FileInfo(f).Length),
                        LastModified = new FileInfo(f).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .OrderByDescending(f => f.Date)
                    .ToList();

                // 如果指定了日期，只返回該日期的檔案
                if (!string.IsNullOrEmpty(date))
                {
                    logFiles = logFiles.Where(f => f.Date == date).ToList();
                }

                return Json(new { success = true, data = logFiles }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"讀取日誌列表失敗: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 讀取日誌內容
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="fileName">檔案名稱</param>
        /// <param name="lines">讀取行數 (預設100行)</param>
        /// <param name="searchText">搜尋文字</param>
        /// <returns></returns>
        public ActionResult ReadLogContent(string userId, string fileName, int lines = 100, string searchText = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(fileName))
                {
                    return Json(new { success = false, message = "參數不完整" }, JsonRequestBehavior.AllowGet);
                }

                var logFilePath = Path.Combine(logBasePath, "UserActions", userId, fileName);
                
                if (!System.IO.File.Exists(logFilePath))
                {
                    return Json(new { success = false, message = "日誌檔案不存在" }, JsonRequestBehavior.AllowGet);
                }

                var logLines = System.IO.File.ReadAllLines(logFilePath);
                
                // 如果有搜尋文字，過濾內容
                if (!string.IsNullOrEmpty(searchText))
                {
                    logLines = logLines.Where(line => line.Contains(searchText)).ToArray();
                }

                // 取最新的指定行數
                var recentLines = logLines.Skip(Math.Max(0, logLines.Length - lines)).Take(lines).ToList();

                return Json(new { 
                    success = true, 
                    data = recentLines, 
                    totalLines = logLines.Length,
                    displayedLines = recentLines.Count
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"讀取日誌內容失敗: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 下載日誌檔案
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="fileName">檔案名稱</param>
        /// <returns></returns>
        public ActionResult DownloadLog(string userId, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(fileName))
                {
                    return new HttpStatusCodeResult(400, "參數不完整");
                }

                var logFilePath = Path.Combine(logBasePath, "UserActions", userId, fileName);
                
                if (!System.IO.File.Exists(logFilePath))
                {
                    return new HttpStatusCodeResult(404, "日誌檔案不存在");
                }

                var fileBytes = System.IO.File.ReadAllBytes(logFilePath);
                var downloadFileName = $"{userId}_{fileName}";

                return File(fileBytes, "text/plain", downloadFileName);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, $"下載日誌檔案失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得所有有日誌記錄的使用者列表
        /// </summary>
        /// <returns></returns>
        private List<string> GetAllLogUsers()
        {
            try
            {
                var userActionsPath = Path.Combine(logBasePath, "UserActions");
                
                if (!Directory.Exists(userActionsPath))
                {
                    return new List<string>();
                }

                return Directory.GetDirectories(userActionsPath)
                    .Select(Path.GetFileName)
                    .OrderBy(u => u)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 格式化檔案大小
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }

        /// <summary>
        /// 取得日誌統計資訊
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="date">日期</param>
        /// <returns></returns>
        public ActionResult GetLogStatistics(string userId, string date)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(date))
                {
                    return Json(new { success = false, message = "參數不完整" }, JsonRequestBehavior.AllowGet);
                }

                var logFilePath = Path.Combine(logBasePath, "UserActions", userId, $"{date}.log");
                
                if (!System.IO.File.Exists(logFilePath))
                {
                    return Json(new { success = false, message = "日誌檔案不存在" }, JsonRequestBehavior.AllowGet);
                }

                var lines = System.IO.File.ReadAllLines(logFilePath);
                var totalLines = lines.Length;
                var requestCount = lines.Count(line => line.Contains("REQUEST |"));
                var responseCount = lines.Count(line => line.Contains("RESPONSE |"));
                var errorCount = lines.Count(line => line.Contains("| ERROR |"));

                // 統計各個 Controller 的使用次數
                var controllerStats = lines
                    .Where(line => line.Contains("REQUEST |"))
                    .Select(line =>
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 4)
                        {
                            var actionPart = parts[3].Trim();
                            var controllerAction = actionPart.Split('.');
                            if (controllerAction.Length >= 2)
                            {
                                return controllerAction[0];
                            }
                        }
                        return "Unknown";
                    })
                    .GroupBy(c => c)
                    .Select(g => new { Controller = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToList();

                var statistics = new
                {
                    TotalLines = totalLines,
                    RequestCount = requestCount,
                    ResponseCount = responseCount,
                    ErrorCount = errorCount,
                    ControllerStats = controllerStats
                };

                return Json(new { success = true, data = statistics }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"取得統計資訊失敗: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}