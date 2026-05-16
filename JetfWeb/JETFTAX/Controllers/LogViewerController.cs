using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// ��x�޲z���
    /// </summary>
    [LoginFilter]
    public class LogViewerController : Controller
    {
        private static readonly Regex LogFileNameRegex = new Regex(
            @"^(?<date>\d{4}-\d{2}-\d{2})\[(?<level>[^\]]+)\]\[(?<userId>[^\]]+)\]\.log$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly string logBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");

        /// <summary>
        /// ��x�˵�����
        /// </summary>
        public ActionResult Index()
        {
            var users = GetAllLogUsers();
            ViewBag.Users = users;
            return View();
        }

        /// <summary>
        /// ���o���w�ϥΪ̪���x�C��
        /// </summary>
        /// <param name="userId">�ϥΪ�ID</param>
        /// <param name="date">��� (�i��)</param>
        /// <returns></returns>
        public ActionResult GetUserLogs(string userId, string date = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "�ϥΪ�ID���ର��" }, JsonRequestBehavior.AllowGet);
                }

                var logFiles = GetLogEntries(userId, date)
                    .Select(f => new
                    {
                        f.FileName,
                        f.Date,
                        f.Level,
                        Size = FormatFileSize(f.Size),
                        LastModified = f.LastModified.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .OrderByDescending(f => f.Date)
                    .ThenByDescending(f => f.LastModified)
                    .ToList();

                if (!logFiles.Any())
                {
                    return Json(new { success = false, message = $"�ϥΪ� {userId} �S����x�O��" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, data = logFiles }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ū����x�C������: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Ū����x���e
        /// </summary>
        /// <param name="userId">�ϥΪ�ID</param>
        /// <param name="fileName">�ɮצW��</param>
        /// <param name="lines">Ū����� (�w�]100��)</param>
        /// <param name="searchText">�j�M��r</param>
        /// <returns></returns>
        public ActionResult ReadLogContent(string userId, string fileName, int lines = 100, string searchText = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(fileName))
                {
                    return Json(new { success = false, message = "�ѼƤ�����" }, JsonRequestBehavior.AllowGet);
                }

                var logFilePath = FindLogFilePath(userId, fileName);
                
                if (string.IsNullOrEmpty(logFilePath) || !System.IO.File.Exists(logFilePath))
                {
                    return Json(new { success = false, message = "��x�ɮפ��s�b" }, JsonRequestBehavior.AllowGet);
                }

                var logLines = System.IO.File.ReadAllLines(logFilePath);
                
                // �p�G���j�M��r�A�L�o���e
                if (!string.IsNullOrEmpty(searchText))
                {
                    logLines = logLines.Where(line => line.Contains(searchText)).ToArray();
                }

                // ���̷s�����w���
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
                return Json(new { success = false, message = $"Ū����x���e����: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// �U����x�ɮ�
        /// </summary>
        /// <param name="userId">�ϥΪ�ID</param>
        /// <param name="fileName">�ɮצW��</param>
        /// <returns></returns>
        public ActionResult DownloadLog(string userId, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(fileName))
                {
                    return new HttpStatusCodeResult(400, "�ѼƤ�����");
                }

                var logFilePath = FindLogFilePath(userId, fileName);
                
                if (string.IsNullOrEmpty(logFilePath) || !System.IO.File.Exists(logFilePath))
                {
                    return new HttpStatusCodeResult(404, "��x�ɮפ��s�b");
                }

                var fileBytes = System.IO.File.ReadAllBytes(logFilePath);
                var downloadFileName = $"{userId}_{fileName}";

                return File(fileBytes, "text/plain", downloadFileName);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, $"�U����x�ɮץ���: {ex.Message}");
            }
        }

        /// <summary>
        /// ���o�Ҧ�����x�O�����ϥΪ̦C��
        /// </summary>
        /// <returns></returns>
        private List<string> GetAllLogUsers()
        {
            try
            {
                if (!Directory.Exists(logBasePath))
                {
                    return new List<string>();
                }

                return GetLogEntries()
                    .Select(x => x.UserId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(u => u)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// �榡���ɮפj�p
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
        /// ���o��x�έp��T
        /// </summary>
        /// <param name="userId">�ϥΪ�ID</param>
        /// <param name="date">���</param>
        /// <returns></returns>
        public ActionResult GetLogStatistics(string userId, string date)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(date))
                {
                    return Json(new { success = false, message = "�ѼƤ�����" }, JsonRequestBehavior.AllowGet);
                }

                var logFilePaths = GetLogEntries(userId, date)
                    .Select(x => x.FilePath)
                    .ToList();

                if (!logFilePaths.Any())
                {
                    return Json(new { success = false, message = "��x�ɮפ��s�b" }, JsonRequestBehavior.AllowGet);
                }

                var lines = logFilePaths
                    .SelectMany(System.IO.File.ReadAllLines)
                    .ToArray();
                var totalLines = lines.Length;
                var requestCount = lines.Count(line => line.Contains("REQUEST |"));
                var responseCount = lines.Count(line => line.Contains("RESPONSE |"));
                var errorCount = lines.Count(line => line.Contains("| ERROR |"));

                // �έp�U�� Controller ���ϥΦ���
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
                return Json(new { success = false, message = $"���o�έp��T����: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

        private string FindLogFilePath(string userId, string fileName)
        {
            return GetLogEntries(userId)
                .Where(x => string.Equals(x.FileName, fileName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.LastModified)
                .Select(x => x.FilePath)
                .FirstOrDefault();
        }

        private List<LogEntryInfo> GetLogEntries(string userId = null, string date = null)
        {
            if (!Directory.Exists(logBasePath))
            {
                return new List<LogEntryInfo>();
            }

            var dateDirectories = string.IsNullOrWhiteSpace(date)
                ? Directory.GetDirectories(logBasePath)
                : new[] { Path.Combine(logBasePath, date) }.Where(Directory.Exists).ToArray();

            var entries = new List<LogEntryInfo>();
            foreach (var dateDirectory in dateDirectories)
            {
                foreach (var levelDirectory in Directory.GetDirectories(dateDirectory))
                {
                    foreach (var filePath in Directory.GetFiles(levelDirectory, "*.log"))
                    {
                        if (!TryParseLogFile(filePath, out var entry))
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(userId) && !string.Equals(entry.UserId, userId, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        entries.Add(entry);
                    }
                }
            }

            return entries;
        }

        private static bool TryParseLogFile(string filePath, out LogEntryInfo entry)
        {
            entry = null;
            var fileName = Path.GetFileName(filePath);
            var match = LogFileNameRegex.Match(fileName ?? string.Empty);
            if (!match.Success)
            {
                return false;
            }

            var info = new FileInfo(filePath);
            entry = new LogEntryInfo
            {
                FilePath = filePath,
                FileName = fileName,
                Date = match.Groups["date"].Value,
                Level = match.Groups["level"].Value,
                UserId = match.Groups["userId"].Value,
                Size = info.Length,
                LastModified = info.LastWriteTime
            };

            return true;
        }

        private sealed class LogEntryInfo
        {
            public string FilePath { get; set; }

            public string FileName { get; set; }

            public string Date { get; set; }

            public string Level { get; set; }

            public string UserId { get; set; }

            public long Size { get; set; }

            public DateTime LastModified { get; set; }
        }
    }
}