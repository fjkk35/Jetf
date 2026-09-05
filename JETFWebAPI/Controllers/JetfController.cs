using FluentFTP;
using JETFWebAPI.Models.SimpleDeclaration;
using JETFWebAPI.Models.Jetf;
using JETFWebAPI.Services;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace JETFWebAPI.Controllers
{
    public class JetfController : ApiController
    {
        /// <summary>
        /// 稅金編號查詢
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public IHttpActionResult TaxNumber([FromBody] TaxNumberModel body)
        {
            string strRequest = "";
            string token = GetHeaders("Token");
            JetfService jetfService = new JetfService();
            TaxNumberResponseModel response = new TaxNumberResponseModel();
            try
            {
                //重複讀取資料流
                System.Web.HttpContext.Current.Request.InputStream.Position = 0;
                using (StreamReader stmReader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    strRequest = System.Web.HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                    stmReader.Close();
                }

                if (ModelState.IsValid)
                {
                    if (jetfService.CheckToken("TaxNumber", strRequest, token))
                    {
                        response = jetfService.PostTaxNumber(body);
                    }
                    else
                    {
                        response.Status = "Fail";
                        response.TrackingNo = body.TrackingNo;
                        response.ResultMessage = "Token驗證錯誤";
                    }
                }
                else
                {
                    response.Status = "Fail";
                    response.ResultMessage = string.Join(";", ModelState.Values
                                      .SelectMany(x => x.Errors)
                                      .Select(x => x.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.TrackingNo = body.TrackingNo;
                response.ResultMessage = ex.Message;
            }

            //寫入LOG紀錄
            GlobalService globalService = new GlobalService();
            globalService.InsertWebAPILog(new Models.Global.WebAPILogModel()
            {
                ControlNmae = "Jetf",
                ActionName = "TaxNumber",
                RequestData = strRequest,
                ResponseData = JsonConvert.SerializeObject(response),
                Remark = token
            });
            return Ok(response);
        }

        /// <summary>
        /// 稅金編號Pdf查詢
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public IHttpActionResult TaxNumberPdf([FromBody] TaxNumberPdfModel body)
        {
            string strRequest = "";
            string token = GetHeaders("Token");
            JetfService jetfService = new JetfService();
            TaxNumberPdfResponseModel response = new TaxNumberPdfResponseModel();
            try
            {
                //重複讀取資料流
                System.Web.HttpContext.Current.Request.InputStream.Position = 0;
                using (StreamReader stmReader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    strRequest = System.Web.HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                    stmReader.Close();
                }

                if (ModelState.IsValid)
                {
                    if (jetfService.CheckToken("TaxNumberPdf", strRequest, token))
                    {
                        response = jetfService.PostTaxNumberPdf(body);
                    }
                    else
                    {
                        response.Status = "Fail";
                        response.TaxNumber = body.TaxNumber;
                        response.ResultMessage = "Token驗證錯誤";
                    }
                }
                else
                {
                    response.Status = "Fail";
                    response.ResultMessage = string.Join(";", ModelState.Values
                                      .SelectMany(x => x.Errors)
                                      .Select(x => x.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.TaxNumber = body.TaxNumber;
                response.ResultMessage = ex.Message;
            }

            //寫入LOG紀錄
            GlobalService globalService = new GlobalService();
            globalService.InsertWebAPILog(new Models.Global.WebAPILogModel()
            {
                ControlNmae = "Jetf",
                ActionName = "TaxNumberPdf",
                RequestData = strRequest,
                ResponseData = JsonConvert.SerializeObject(response),
                Remark = token
            });
            return Ok(response);
        }

        /// <summary>
        /// 取得稅金編號Pdf
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage GetTaxNumberPdf(string taxNumber, string token)
        {
            HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            JetfService jetfService = new JetfService();
            if (jetfService.CheckToken("GetTaxNumberPdf", taxNumber, token))
            {
                FtpClient ftp = new FtpClient();
                ftp.Host = "192.168.1.5";
                ftp.Credentials = new NetworkCredential("tax_user", "a5d+46b2j59");
                ftp.Connect();

                string filePath = jetfService.GetClearance_Tax_Pdf(taxNumber);
                byte[] b;
                ftp.DownloadBytes(out b, filePath); //下載FTP檔案
                MemoryStream stream = new MemoryStream(b);
                stream.Position = 0;

                if (stream == null)
                {
                    return Request.CreateResponse(System.Net.HttpStatusCode.NotFound);
                }
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline"); //"inline" to make it appear on the browser //"attachment" for direct download
                result.Content.Headers.ContentDisposition.FileName = "file.pdf";
                result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                result.Content.Headers.ContentLength = stream.Length;

                ftp.Dispose();
                //stream.Dispose();
            }
            return result;
        }

        /// <summary>
        /// 簡易報單Pdf url 查詢
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public IHttpActionResult SimpleDeclarationPdf([FromBody] SimpleDeclarationPdfModel body)
        {
            string strRequest = "";
            string token = GetHeaders("Token");
            var _simpleDeclarationService = new SimpleDeclarationService();

            try
            {
                //重複讀取資料流
                System.Web.HttpContext.Current.Request.InputStream.Position = 0;
                using (StreamReader stmReader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    strRequest = System.Web.HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                    stmReader.Close();
                }

#if !DEBUG
                if (_simpleDeclarationService.CheckToken("SimpleDeclarationPdf", strRequest, token) == false)
                {
                    return Ok(new SimpleDeclarationPdfResponseModel()
                    {
                        Status = "Fail",
                        TrackingNo = body.TrackingNo,
                        ResultMessage = "Token驗證錯誤"
                    });
                }
#endif
                var response = _simpleDeclarationService.PostSimpleDeclarationPdf(body);

                //寫入LOG紀錄
                var globalService = new GlobalService();
                globalService.InsertWebAPILog(new Models.Global.WebAPILogModel()
                {
                    ControlNmae = "Jetf",
                    ActionName = "SimpleDeclarationPdf",
                    RequestData = strRequest,
                    ResponseData = JsonConvert.SerializeObject(response),
                    Remark = token
                });

                return Ok(response);

            }
            catch (Exception ex)
            {
                return Ok(new SimpleDeclarationPdfResponseModel()
                {
                    Status = "Fail",
                    TrackingNo = body.TrackingNo,
                    ResultMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// 取得簡易報單Pdf
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage GetSimpleDeclarationPdf(string trackingNo, string token) 
        {
            var _simpleDeclarationService = new SimpleDeclarationService();

            HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            JetfService jetfService = new JetfService();

            if (!jetfService.CheckToken("GetSimpleDeclarationPdf", trackingNo, token))
                return result;

            var bytes = _simpleDeclarationService.GetSimpleDeclarationPdf(trackingNo);

            if (bytes == null)
            {
                return Request.CreateResponse(System.Net.HttpStatusCode.NotFound);
            }


            MemoryStream stream = new MemoryStream(bytes);
            stream.Position = 0;

            if (stream == null)
            {
                return Request.CreateResponse(System.Net.HttpStatusCode.NotFound);
            }

            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline"); //"inline" to make it appear on the browser //"attachment" for direct download
            result.Content.Headers.ContentDisposition.FileName = "file.pdf";
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            result.Content.Headers.ContentLength = stream.Length;

            return result;
        }

        /// <summary>
        /// 簽收單查詢
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public IHttpActionResult CargoSignReceipt([FromBody] CargoSignReceiptModel body)
        {
            string strRequest = "";
            string token = GetHeaders("Token");
            JetfService jetfService = new JetfService();
            CargoSignReceiptResponseModel response = new CargoSignReceiptResponseModel();
            try
            {
                //重複讀取資料流
                System.Web.HttpContext.Current.Request.InputStream.Position = 0;
                using (StreamReader stmReader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    strRequest = System.Web.HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                    stmReader.Close();
                }

                if (ModelState.IsValid)
                {
                    if (jetfService.CheckToken("CargoSignReceipt", strRequest, token))
                    {
                        response = jetfService.PostCargoSignReceipt(body);
                    }
                    else
                    {
                        response.Status = "Fail";
                        response.CargoNumber = body.CargoNumber;
                        response.ResultMessage = "Token驗證錯誤";
                    }
                }
                else
                {
                    response.Status = "Fail";
                    response.ResultMessage = string.Join(";", ModelState.Values
                                      .SelectMany(x => x.Errors)
                                      .Select(x => x.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                response.Status = "Fail";
                response.CargoNumber = body.CargoNumber;
                response.ResultMessage = ex.Message;
            }

            //寫入LOG紀錄
            GlobalService globalService = new GlobalService();
            globalService.InsertWebAPILog(new Models.Global.WebAPILogModel()
            {
                ControlNmae = "Jetf",
                ActionName = "CargoSignReceipt",
                RequestData = strRequest,
                ResponseData = JsonConvert.SerializeObject(response),
                Remark = token
            });

            return Ok(response);
        }

        /// <summary>
        /// 取得簽收單圖片
        /// </summary>
        /// <param name="invoice"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage GetCargoSignReceipt(string cargoNumber,string fileName, string token)
        {
            HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            JetfService jetfService = new JetfService();
            if (jetfService.CheckToken("GetCargoSignReceipt", cargoNumber, token))
            {
                FtpClient ftp = new FtpClient();
                ftp.Host = "192.168.1.5";
                ftp.Credentials = new NetworkCredential("sign_user", "b9Q5-841ph66");
                ftp.Connect();

                string filePath = jetfService.GetCargo_Sign_Receipt(cargoNumber, fileName);
                byte[] b;
                ftp.DownloadBytes(out b, filePath); //下載FTP檔案
                //馬賽克
                b = AdjustTobMosaic(b, 20);

                MemoryStream stream = new MemoryStream(b);
                stream.Position = 0;

                if (stream == null)
                {
                    return Request.CreateResponse(System.Net.HttpStatusCode.NotFound);
                }
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline"); //"inline" to make it appear on the browser //"attachment" for direct download
                result.Content.Headers.ContentDisposition.FileName = "file.jpg";
                result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpg");
                result.Content.Headers.ContentLength = stream.Length;

                ftp.Dispose();
                //stream.Dispose();
            }
            return result;
        }

        /// <summary>
        /// 尾程配送基础资料
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public IHttpActionResult ShippingInformation([FromBody] ShippingInformationModel body)
        {
            string strRequest = "";
            string token = GetHeaders("Token");
            ShippingInformationResponseModel response = new ShippingInformationResponseModel();
            JetfService jetfService = new JetfService();
            try
            {
                //重複讀取資料流
                System.Web.HttpContext.Current.Request.InputStream.Position = 0;
                using (StreamReader stmReader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    strRequest = System.Web.HttpUtility.HtmlDecode(stmReader.ReadToEnd().Trim());
                    stmReader.Close();
                }

                if (ModelState.IsValid)
                {
                    if (jetfService.CheckToken("ShippingInformation", strRequest, token))
                    {
                        response = jetfService.ShippingInformation(body);
                    }
                    else
                    {
                        response.Status = "false";
                        response.Message = "Token驗證錯誤";
                    }
                }
                else
                {
                    response.Status = "false";
                    response.Message = string.Join(";", ModelState.Values
                                      .SelectMany(x => x.Errors)
                                      .Select(x => x.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                response.Status = "false";
                response.Message = ex.Message;
            }

            return Ok(response);
        }

        /// <summary>
        /// 馬賽克處理圖片
        /// </summary>
        /// <param name="byteData">原圖片</param>
        /// <param name="sImgPath">加碼圖片</param>
        /// <param name="effectWidth"> 影響範圍 每一個格子數</param>
        public byte[] AdjustTobMosaic(byte[] byteData, int effectWidth)
        {
            //設置馬賽克百分比寬高(0~1f)
            Single maWidth = 1f, maHeight = 1f;
            using (MemoryStream ms = new MemoryStream(byteData))
            {
                using (Bitmap bitmap = new Bitmap(ms))
                {
                    try
                    {
                        // 差異最多的就是以照一定範圍取樣 之後直接去下一個範圍
                        for (int heightOfffset = 0; heightOfffset < bitmap.Height * maHeight; heightOfffset += effectWidth)//可以調整大碼區域，調整打碼寬高
                        {
                            for (int widthOffset = 0; widthOffset < bitmap.Width * maWidth; widthOffset += effectWidth)
                            {
                                int avgR = 0, avgG = 0, avgB = 0;
                                int blurPixelCount = 0;

                                for (int x = widthOffset; (x < widthOffset + effectWidth && x < bitmap.Width); x++)
                                {
                                    for (int y = heightOfffset; (y < heightOfffset + effectWidth && y < bitmap.Height); y++)
                                    {
                                        System.Drawing.Color pixel = bitmap.GetPixel(x, y);
                                        avgR += pixel.R;
                                        avgG += pixel.G;
                                        avgB += pixel.B;
                                        blurPixelCount++;
                                    }
                                }
                                // 計算範圍平均
                                avgR = avgR / blurPixelCount;
                                avgG = avgG / blurPixelCount;
                                avgB = avgB / blurPixelCount;
                                // 所有範圍內都設定此值
                                for (int x = widthOffset; (x < widthOffset + effectWidth && x < bitmap.Width); x++)
                                {
                                    for (int y = heightOfffset; (y < heightOfffset + effectWidth && y < bitmap.Height); y++)
                                    {
                                        if ((y > 60 && y < 190 && x > 100 && x < 280) ||
                                            (y > 40 && y < 190 && x > 340 && x < 580))
                                        {
                                            System.Drawing.Color newColor = System.Drawing.Color.FromArgb(avgR, avgG, avgB);
                                            bitmap.SetPixel(x, y, newColor);
                                        }
                                    }
                                }
                            }
                        }
                        //保存文件
                        //bitmap.Save(sImgPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    catch (Exception ex)
                    {
                        return byteData;
                    }

                    return ToByteArray(bitmap, ImageFormat.Jpeg);
                }
            }
        }

        public byte[] ToByteArray(Image image, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

        public string GetHeaders(string key)
        {
            var headers = Request.Headers;
            string value = "";
            if (headers.Contains(key))
            {
                value = headers.GetValues(key).First();
            }
            return value;
        }
    }
}
