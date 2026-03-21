using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class CaptchaController : Controller
    {
        /// <summary>
        /// 取得圖型驗證碼
        /// </summary>
        /// <param name="i">丟入不同變數讓畫面不被cache，刷新圖片用</param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult GetValidateCode(string key, string i = "")
        {
            byte[] data = null;
            string code = RandomCode(5);
            TempData["code" + key] = code;
            //定義一個畫板
            MemoryStream ms = new MemoryStream();
            using (Bitmap map = new Bitmap(100, 40))
            {
                //畫筆,在指定畫板畫板上畫圖
                //g.Dispose();
                using (Graphics g = Graphics.FromImage(map))
                {
                    g.Clear(Color.White);
                    g.DrawString(code, new Font("黑體", 18.0F), Brushes.Blue, new Point(10, 8));
                    //繪製干擾線(數字代表幾條)
                    PaintInterLine(g, 10, map.Width, map.Height);
                }
                map.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            data = ms.GetBuffer();
            return File(data, "image/jpeg");
        }

        /// <summary>
        /// 隨機生成指定長度的驗證碼字符串
        /// </summary>
        /// <param name="length">驗證碼長度</param>
        /// <returns></returns>
        private string RandomCode(int length)
        {
            //string s = "0123456789zxcvbnmasdfghjklqwertyuiop";
            string s = "0123456789";
            StringBuilder sb = new StringBuilder();
            Random rand = new Random();
            int index;
            for (int i = 0; i < length; i++)
            {
                index = rand.Next(0, s.Length);
                sb.Append(s[index]);
            }
            return sb.ToString();
        }

        /// <summary>
        ///  產生刪除線 num 代表幾條
        /// </summary>
        /// <param name="g"></param>
        /// <param name="num">幾條</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void PaintInterLine(Graphics g, int num, int width, int height)
        {
            Random r = new Random();
            int startX, startY, endX, endY;
            for (int i = 0; i < num; i++)
            {
                startX = r.Next(0, width);
                startY = r.Next(0, height);
                endX = r.Next(0, width);
                endY = r.Next(0, height);
                g.DrawLine(new Pen(Brushes.Red), startX, startY, endX, endY);
            }
        }
    }
}