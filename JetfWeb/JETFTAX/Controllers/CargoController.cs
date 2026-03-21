using FluentFTP;
using JETFTAX.Models;
using JETFTAX.Models.Cargo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CargoController : Controller
    {
        GlobalService globalService = new GlobalService();
        CargoService cargoService = new CargoService();
        CustomerService customerService = new CustomerService();

        IFont fontB;
        XSSFDataFormat format;
        XSSFFont font1;
        XSSFCellStyle cs_Title, cs_Title_Left, cs_Center, cs_Center_Blue, cs_Int, cs_Int_Blue, cs_Double, cs_Percent2, dateStyle, date2Style;

        /// <summary>
        /// 稅金查詢
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchTax)]
        public ActionResult SearchCargo()
        {
            return View();
        }

        /// <summary>
        /// 稅金查詢-資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchTax)]
        public ActionResult GetFee_Master(string data)
        {
            JObject obj = JObject.Parse(data);
            string invoice = obj["invoice"].Value<string>();

            DataTable dt_Fee_Master = cargoService.GetFee_Master(invoice);
            int count = dt_Fee_Master.Rows.Count;
            JDataTableModel model = new JDataTableModel()
            {
                recordsTotal = count,
                recordsFiltered = count,
                data = JsonConvert.SerializeObject(dt_Fee_Master)
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 稅金查詢-明細、物流狀態
        /// </summary>
        /// <param name="trans_number"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchTax)]
        public ActionResult DialogCargo(string trans_number)
        {
            int tax1, tax2;
            DateTime in_Datetime, out_Datetime;

            DialogCargoViewModel vm = new DialogCargoViewModel();
            vm.DialogCargoList = new List<Models.DialogCargo>();
            //明細
            DataTable dt_Fee_Master = cargoService.GetFee_Master(trans_number);
            if (dt_Fee_Master.Rows.Count > 0)
            {
                vm.Source = dt_Fee_Master.Rows[0]["Source"].ToString();
                vm.Type = dt_Fee_Master.Rows[0]["Type"].ToString();
                vm.Main_Number = dt_Fee_Master.Rows[0]["Main_Number"].ToString();
                vm.Bag_Number = dt_Fee_Master.Rows[0]["Bag_Number"].ToString();
                vm.Tax_Number = dt_Fee_Master.Rows[0]["Tax_Number"].ToString();
                vm.Cust_Name = dt_Fee_Master.Rows[0]["Cust_Name"].ToString();
                vm.Trans_Name = dt_Fee_Master.Rows[0]["Trans_Name"].ToString();
                vm.Dlv_Inv = dt_Fee_Master.Rows[0]["Dlv_Inv"].ToString();
                vm.Recipient = dt_Fee_Master.Rows[0]["Recipient"].ToString();
                vm.Recphone = dt_Fee_Master.Rows[0]["Recphone"].ToString();
                vm.Recaddress = dt_Fee_Master.Rows[0]["Recaddress"].ToString();
                if (DateTime.TryParse(dt_Fee_Master.Rows[0]["In_Datetime"].ToString(), out in_Datetime))
                {
                    vm.In_Datetime = in_Datetime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                if (DateTime.TryParse(dt_Fee_Master.Rows[0]["Out_Datetime"].ToString(), out out_Datetime))
                {
                    vm.Out_Date = out_Datetime.ToString("yyyy-MM-dd");
                    vm.Out_Datetime = out_Datetime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                vm.Include_Tax = dt_Fee_Master.Rows[0]["INCLUDE_TAX"].ToString();
                tax1 = 0;
                tax2 = 0;
                Int32.TryParse(dt_Fee_Master.Rows[0]["Tax1"].ToString(), out tax1);
                Int32.TryParse(dt_Fee_Master.Rows[0]["Tax2"].ToString(), out tax2);
                vm.Tax1 = tax1.ToString();
                vm.Tax2 = tax2.ToString();
                vm.TotalTax = (tax1 + tax2).ToString();
                vm.CCFee = dt_Fee_Master.Rows[0]["CCFee"].ToString();
                vm.Fee = dt_Fee_Master.Rows[0]["Fee"].ToString();
                vm.Cod = dt_Fee_Master.Rows[0]["Cod"].ToString();
                vm.To_Dlv_Cod = dt_Fee_Master.Rows[0]["To_Dlv_Cod"].ToString();
            }

            //配送進度
            DataTable dt_Cargo_Status = cargoService.GetCargo_Status_Detail(trans_number);

            for (int i = 0; i < dt_Cargo_Status.Rows.Count; i++)
            {
                vm.DialogCargoList.Add(new Models.DialogCargo()
                {
                    tran_modify_time = dt_Cargo_Status.Rows[i]["TRANS_MODIFY_TIME"].ToString() != "" ? Convert.ToDateTime(dt_Cargo_Status.Rows[i]["TRANS_MODIFY_TIME"]).ToString("yyyy-MM-dd HH:mm:ss") : "",
                    tran_status = dt_Cargo_Status.Rows[i]["TRANS_STATUS_DESC"].ToString()
                });
            }

            //紀錄LOG
            cargoService.InsertLog_Cargo_Status(new LogCargoStatusModel()
            {
                Dlv_Inv = trans_number,
                Remark = "貨況查詢",
                Search_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                User_Ip = globalService.GetIPAddress(),
                User_Id = Session["user_id"].ToString()
            });

            return PartialView(vm);
        }

        /// <summary>
        /// 貨況查詢
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult SearchCargo2()
        {
            SearchCargo2ViewModel vm = new SearchCargo2ViewModel();
            List<SelectListItem> searchTypeList = new List<SelectListItem>();
            searchTypeList.Add(new SelectListItem() { Text = "分提單號", Value = "trackingNo" });
            searchTypeList.Add(new SelectListItem() { Text = "物流貨號", Value = "invoice" });
            searchTypeList.Add(new SelectListItem() { Text = "手機", Value = "phone" });
            searchTypeList.Add(new SelectListItem() { Text = "客戶外箱號", Value = "fieldX" });
            searchTypeList.Add(new SelectListItem() { Text = "客戶訂單號", Value = "orderNo" });

            vm.ddlSearchTypeList = searchTypeList;
            return View(vm);
        }

        /// <summary>
        /// 貨況查詢-資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult GetMerge_Originallist(string data)
        {
            DataTable dt_Merge_Originallist = new DataTable();
            JObject obj = JObject.Parse(data);
            string invoice = obj["invoice"].Value<string>().Trim();
            string searchType = obj["searchType"].Value<string>();

            if (invoice != "")
            {
                switch (searchType)
                {
                    case "phone":
                        dt_Merge_Originallist = cargoService.GetMerge_Originallist_Phone(invoice);
                        break;
                    case "invoice":
                        dt_Merge_Originallist = cargoService.GetMerge_Originallist_Deliveryno(invoice);
                        if (dt_Merge_Originallist.Rows.Count == 0)
                        {
                            var trackingNo = cargoService.GetShenzhenCargoTrackingNo(invoice);
                            if (!string.IsNullOrEmpty(trackingNo))
                            {
                                dt_Merge_Originallist = cargoService.GetMerge_Originallist_Bl_No(trackingNo);
                            }
                        }
                        break;
                    case "trackingNo":
                        dt_Merge_Originallist = cargoService.GetMerge_Originallist_Jetf_Serial(invoice);
                        if (dt_Merge_Originallist.Rows.Count == 0)
                        {
                            dt_Merge_Originallist = cargoService.GetMerge_Originallist_Bl_No(invoice);
                        }
                        break;
                    case "fieldX":
                        string bagNo = cargoService.GetOriginallist_BagNo(invoice);
                        if (bagNo != "")
                        {
                            dt_Merge_Originallist = cargoService.GetMerge_Originallist_Bl_No(bagNo);
                        }
                        break;
                    case "orderNo":
                        string deliveryno = cargoService.GetOriginallist_Deliveryno(invoice);
                        if (deliveryno != "")
                        {
                            dt_Merge_Originallist = cargoService.GetMerge_Originallist_Deliveryno(deliveryno);
                        }
                        break;
                }
            }

            int count = dt_Merge_Originallist.Rows.Count;
            JDataTableModel model = new JDataTableModel()
            {
                recordsTotal = count,
                recordsFiltered = count,
                data = JsonConvert.SerializeObject(dt_Merge_Originallist)
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 貨況查詢-明細
        /// </summary>
        /// <param name="trans_number"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult DialogCargo2(string id)
        {
            string taxNumber, original;
            int tax1, tax2;
            DateTime in_Datetime, out_Datetime, ETA;

            DialogCargoViewModel vm = new DialogCargoViewModel();
            vm.DialogCargoList = new List<Models.DialogCargo>();
            //明細
            DataTable dt_Merge_Originallist = cargoService.GetMerge_Originallist_Id(id);
            if (dt_Merge_Originallist.Rows.Count > 0)
            {
                //資料來源 空運、海運
                original = dt_Merge_Originallist.Rows[0]["ORIGINAL"].ToString();

                vm.Id = dt_Merge_Originallist.Rows[0]["ID"].ToString();

                if (DateTime.TryParse(dt_Merge_Originallist.Rows[0]["ETA"].ToString(), out ETA))
                {
                    vm.ETA = ETA.ToString("yyyy-MM-dd");
                }
                vm.GW = dt_Merge_Originallist.Rows[0]["GW"].ToString();
                vm.PIECE = dt_Merge_Originallist.Rows[0]["PIECE"].ToString();
                vm.Source = dt_Merge_Originallist.Rows[0]["I_DATA_TYPE"].ToString();
                vm.Type = dt_Merge_Originallist.Rows[0]["I_CLEARANCE_TYPE"].ToString();
                vm.Main_Number = dt_Merge_Originallist.Rows[0]["MAINNUMBER"].ToString();
                vm.Bag_Number = dt_Merge_Originallist.Rows[0]["BL_NO"].ToString();
                vm.Tax_Number = dt_Merge_Originallist.Rows[0]["F_TAX_NUMBER"].ToString();
                vm.Cust_Id = dt_Merge_Originallist.Rows[0]["DESPATCH_NAME"].ToString();
                vm.Cust_Name = dt_Merge_Originallist.Rows[0]["CUSTOMER"].ToString();
                vm.Trans_Name = dt_Merge_Originallist.Rows[0]["TRANS_NAME"].ToString();
                vm.Trans_Name_New = dt_Merge_Originallist.Rows[0]["TRANS_NAME_NEW"].ToString();
                vm.Dlv_Inv = dt_Merge_Originallist.Rows[0]["JETF_SERIAL"].ToString();
                vm.Deliveryno = dt_Merge_Originallist.Rows[0]["DELIVERYNO"].ToString();
                vm.Recipient = dt_Merge_Originallist.Rows[0]["IMPORTER"].ToString();
                vm.Recphone = dt_Merge_Originallist.Rows[0]["IM_PHONENO"].ToString();
                vm.Recaddress = dt_Merge_Originallist.Rows[0]["IM_ADD"].ToString();
                vm.CC = dt_Merge_Originallist.Rows[0]["CC"].ToString();
                vm.Field_X = dt_Merge_Originallist.Rows[0]["FIELD_X"].ToString();
                if (DateTime.TryParse(dt_Merge_Originallist.Rows[0]["I_SIGN_IN_TIME"].ToString(), out in_Datetime))
                {
                    vm.In_Datetime = in_Datetime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                if (DateTime.TryParse(dt_Merge_Originallist.Rows[0]["I_SIGN_OUT_TIME"].ToString(), out out_Datetime))
                {
                    vm.Out_Date = out_Datetime.ToString("yyyy-MM-dd");
                    vm.Out_Datetime = out_Datetime.ToString("yyyy-MM-dd HH:mm:ss");
                }

                //取得稅金資料
                var feeMaster = cargoService.GetFeeMaster(vm.Deliveryno);

                if (feeMaster == null)
                {
                    feeMaster = cargoService.GetFeeMaster(vm.Dlv_Inv);
                }

            
                vm.Include_Tax = feeMaster?.IncludeTax;
                vm.Tax1 = feeMaster?.Tax1.ToString();
                vm.Tax2 = feeMaster?.Tax2.ToString();
                vm.TotalTax = feeMaster?.TotalTax.ToString();
                vm.CCFee = feeMaster?.CcFee.ToString();
                vm.Fee = feeMaster?.Fee.ToString();
                vm.Cod = feeMaster?.Cod.ToString();
                vm.To_Dlv_Cod = feeMaster?.ToDlvCod.ToString();
                vm.CustomerCod = feeMaster?.CustomerCod.ToString();
                vm.TransCod = feeMaster?.TransCod.ToString();

                vm.Order_No = dt_Merge_Originallist.Rows[0]["ORDER_NO"].ToString();
                vm.Express_No = dt_Merge_Originallist.Rows[0]["EXPRESS_NO"].ToString();
                vm.TrackingNo = dt_Merge_Originallist.Rows[0]["TRACKINGNO"].ToString();

                //取得稅單編號
                vm.TaxNumberList = new List<TaxNumberItem>();
                DataTable dt_TaxNumber = cargoService.GetTaxNumber(original, vm.Bag_Number, vm.Dlv_Inv);
                for (int i = 0; i < dt_TaxNumber.Rows.Count; i++)
                {
                    taxNumber = dt_TaxNumber.Rows[i]["TAX_NUMBER"].ToString();
                    vm.TaxNumberList.Add(new TaxNumberItem()
                    {
                        TaxNumber = taxNumber
                    });
                }
                //取得掃貨上車掃讀時間、掃讀人員
                DataTable dt_ScanCargo = cargoService.GetPdtScanCargoUpload(vm.Bag_Number, vm.Dlv_Inv);
                if (dt_ScanCargo.Rows.Count > 0)
                {
                    vm.ScanCargoUploadTime = Convert.ToDateTime(dt_ScanCargo.Rows[0]["UploadTime"]).ToString("yyyy-MM-dd HH:mm:ss");
                    vm.ScanCargoUploadOpe = dt_ScanCargo.Rows[0]["UploadOpe"].ToString().Trim();
                    vm.ScanCargoTransName = dt_ScanCargo.Rows[0]["TransName"].ToString().Trim();
                    vm.ScanCargoCarNo = dt_ScanCargo.Rows[0]["CarNo"].ToString().Trim();
                };

                //取得錯單類別
                DataTable dt_ErrorReason = cargoService.GetErrorReason(original, vm.Main_Number, vm.Bag_Number, vm.Dlv_Inv);
                var reason = string.Join("，",
                       dt_ErrorReason.AsEnumerable()
                       .Select(r => r.Field<string>("Reason"))
                       .Distinct()
                       .ToList());
                vm.ErrorReason = reason;

                //狀態
                if (original.ToUpper() == "ETL")
                {
                    //空運
                    vm.Status = cargoService.GetEtlStatus(vm.TrackingNo);
                }
                else 
                {
                    //海運
                    var status = dt_Merge_Originallist.Rows[0]["Status"].ToString();
                    vm.Status = status == "D" ? "出口地扣留" : "";
                }


                    //配送進度
                    DataTable dt_Cargo_Status = cargoService.GetCargo_Status_Detail(vm.Deliveryno);

                for (int i = 0; i < dt_Cargo_Status.Rows.Count; i++)
                {
                    vm.DialogCargoList.Add(new Models.DialogCargo()
                    {
                        tran_modify_time = dt_Cargo_Status.Rows[i]["TRANS_MODIFY_TIME"].ToString() != "" ? Convert.ToDateTime(dt_Cargo_Status.Rows[i]["TRANS_MODIFY_TIME"]).ToString("yyyy-MM-dd HH:mm:ss") : "",
                        tran_status = dt_Cargo_Status.Rows[i]["TRANS_STATUS_DESC"].ToString()
                    });
                }

                //紀錄LOG
                cargoService.InsertLog_Cargo_Status(new LogCargoStatusModel()
                {
                    Dlv_Inv = vm.Dlv_Inv,
                    Remark = "貨況查詢",
                    Search_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    User_Ip = globalService.GetIPAddress(),
                    User_Id = Session["user_id"].ToString()
                });
            }

            return PartialView(vm);
        }

        /// <summary>
        /// 貨況查詢-明細-處置說明資料
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult GetProcess(string data)
        {
            DataTable dt = cargoService.GetProcess(data);
            int count = dt.Rows.Count;
            JDataTableModel model = new JDataTableModel()
            {
                recordsTotal = count,
                recordsFiltered = count,
                data = JsonConvert.SerializeObject(dt)
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 貨況查詢-新增處置說明
        /// </summary>
        /// <param name="trans_number"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult DialogProcess()
        {
            return PartialView();
        }

        /// <summary>
        /// 貨況查詢-上傳處置說明附件檔案
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public JsonResult UploadFileProcess(HttpPostedFileBase file, DialogProcessViewModel vm)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            try
            {
                if (vm.P_Remark == null || vm.P_Remark.Trim() == "")
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "請輸入處置說明";
                    return Json(resopnseModel, JsonRequestBehavior.AllowGet);
                }

                ProcessModel model = new ProcessModel()
                {
                    MId = vm.P_MId,
                    Process_Type = vm.P_Type,
                    DataDate = DateTime.Now.ToString("yyyyMMdd"),
                    Dlv_Inv = vm.P_Dlv_Inv,
                    Remark = vm.P_Remark,
                    User_Id = Session["user_id"].ToString()
                };

                if (file != null && file.ContentLength > 0)
                {
                    var fileType = Path.GetExtension(file.FileName);
                    var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                    //filePath = Path.Combine(Server.MapPath("~/UploadProcess"), fileName);
                    var filePath = Path.Combine(@"D:\UploadProcess", fileName);
                    file.SaveAs(filePath);
                    //寫入檔名、路徑
                    model.FileName = file.FileName;
                    model.FilePath = filePath;
                }

                //寫入資料
                return Json(cargoService.InsertProcess(model), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
                return Json(resopnseModel, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 貨況查詢-刪除處置說明
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult DeleteProcess(string id)
        {
            ResopnseModel resopnseModel = cargoService.DeleteProcess(id, Session["user_id"].ToString());
            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 貨況查詢-處置說明結案
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult FinishProcess(string dlv_inv)
        {
            ResopnseModel resopnseModel = cargoService.FinishProcess(dlv_inv, Session["user_id"].ToString());
            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 貨況查詢-下載處置說明檔案
        /// </summary>
        /// <param name="year"></param>
        /// <param name="prjId"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult DownloadFile(string filePath, string fileName)
        {
            //try
            //{
            FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "application/octet-stream", fileName); //MME 格式 可上網查 此為通用設定
                                                                       //}
                                                                       //catch (System.Exception)
                                                                       //{
                                                                       //return Content("<script>alert('查無此檔案');window.close()</script>");
                                                                       //}
        }

        /// <summary>
        /// 貨況查詢-取得稅金編號Pdf
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult GetTaxNumberPdf(string taxNumber)
        {
            try
            {
                //紀錄LOG
                cargoService.InsertLog_Cargo_Status(new LogCargoStatusModel()
                {
                    Dlv_Inv = taxNumber,
                    Remark = "稅單查詢",
                    Search_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    User_Ip = globalService.GetIPAddress(),
                    User_Id = Session["user_id"].ToString()
                });

                HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                FtpClient ftp = new FtpClient();
                ftp.Host = "192.168.1.5";
                ftp.Credentials = new NetworkCredential("tax_user", "a5d+46b2j59");
                ftp.Connect();
                //取得稅單Pdf路徑
                string filePath = cargoService.GetClearance_Tax_Pdf(taxNumber);
                if (filePath == "")
                {
                    return Content("查無資料");
                }
                else
                {
                    byte[] content;
                    ftp.DownloadBytes(out content, filePath); //下載FTP檔案
                    MemoryStream stream = new MemoryStream(content);
                    stream.Position = 0;
                    ftp.Dispose();
                    Response.AppendHeader("Content-Disposition", "inline; filename=稅單.pdf;");
                    return File(content, "application/pdf");
                }
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        /// <summary>
        /// 貨況查詢-查看簽收單圖片
        /// </summary>
        /// <param name="cargoNumber"></param>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult CargoSignReceipt(string cargoNumber)
        {
            CargoSignReceiptViewModel vm = new CargoSignReceiptViewModel();
            vm.UrlList = new List<UrlItem>();
            string filePath;
            byte[] content;
            FtpClient ftp = new FtpClient();
            ftp.Host = "192.168.1.5";
            ftp.Credentials = new NetworkCredential("sign_user", "b9Q5-841ph66");
            ftp.Connect();
            //簽收單路徑
            DataTable dt = cargoService.GetCargo_Sign_Receipt(cargoNumber);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                content = null;
                filePath = dt.Rows[i]["FilePath"].ToString();
                ftp.DownloadBytes(out content, filePath); //下載FTP檔案
                if (content != null)
                {
                    vm.UrlList.Add(new UrlItem()
                    {
                        Url = Convert.ToBase64String(content)
                    });
                }
            }
            ftp.Dispose();
            //紀錄LOG
            cargoService.InsertLog_Cargo_Status(new LogCargoStatusModel()
            {
                Dlv_Inv = cargoNumber,
                Remark = "簽收單查詢",
                Search_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                User_Ip = globalService.GetIPAddress(),
                User_Id = Session["user_id"].ToString()
            });

            return View(vm);
        }

        /// <summary>
        /// 貨況查詢-查詢記錄
        /// </summary>
        /// <param name="trans_number"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult DialogLogCargoStatus(string dlv_inv)
        {
            DialogLogCargoStatusViewModel vm = new DialogLogCargoStatusViewModel();
            vm.DialogLogCargoStatusList = new List<Models.DialogLogCargoStatus>();
            DataTable dt = cargoService.GetLog_Cargo_Status(dlv_inv);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                vm.DialogLogCargoStatusList.Add(new Models.DialogLogCargoStatus()
                {
                    Dlv_Inv = dt.Rows[i]["DLV_INV"].ToString(),
                    Search_Time = Convert.ToDateTime(dt.Rows[i]["SEARCH_TIME"]).ToString("yyyy-MM-dd HH:mm:ss"),
                    User_Id = dt.Rows[i]["USER_ID"].ToString(),
                    User_Ip = dt.Rows[i]["USER_IP"].ToString()
                });
            }
            return PartialView(vm);
        }

        /// <summary>
        /// 貨況查詢-清關袋號
        /// </summary>
        /// <param name="trans_number"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult DialogCargoTargetBagNumber(string bagNumber)
        {
            TargetBagNumberViewModel vm = new TargetBagNumberViewModel();
            vm.List = new List<TargetBagNumber>();
            DataTable dt = cargoService.GetTargetBagNumber(bagNumber);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var item = new TargetBagNumber();

                //通關袋號
                item.TargetCode = dt.Rows[i]["TARGET_CODE"].ToString();
                //袋號
                item.SourceCode = dt.Rows[i]["SOURCE_CODE"].ToString();

                if (DateTime.TryParse(dt.Rows[i]["SIGN_IN_TIME"].ToString(), out var signInTime))
                {
                    item.SignInTime = signInTime.ToString("yyyy-MM-dd HH:mm:ss");
                }

                if (DateTime.TryParse(dt.Rows[i]["SIGN_OUT_TIME"].ToString(), out var signOutTime))
                {
                    item.SignOutTime = signOutTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                vm.List.Add(item);
            }
            return PartialView(vm);
        }

        /// <summary>
        /// 貨況查詢-併分提單號
        /// </summary>
        /// <param name="trans_number"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult DialogCargoTargetTrackingNo(string bagNumber)
        {
            TargetBagNumberViewModel vm = new TargetBagNumberViewModel();
            vm.List = new List<TargetBagNumber>();
            DataTable dt = cargoService.GetTargetTrackingNo(bagNumber);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var item = new TargetBagNumber();

                //通關袋號
                item.TargetCode = dt.Rows[i]["TARGET_CODE"].ToString();
                //袋號
                item.SourceCode = dt.Rows[i]["SOURCE_CODE"].ToString();

                if (DateTime.TryParse(dt.Rows[i]["SIGN_IN_TIME"].ToString(), out var signInTime))
                {
                    item.SignInTime = signInTime.ToString("yyyy-MM-dd HH:mm:ss");
                }

                if (DateTime.TryParse(dt.Rows[i]["SIGN_OUT_TIME"].ToString(), out var signOutTime))
                {
                    item.SignOutTime = signOutTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                vm.List.Add(item);
            }
            return PartialView(vm);
        }

        /// <summary>
        /// 貨況查詢-速派新遞貨號
        /// </summary>
        /// <param name="trans_number"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.SearchCargo)]
        public ActionResult DialogShenzhenCargo(string bagNumber)
        {
            ShenzhenCargoViewModel vm = new ShenzhenCargoViewModel();
            vm.List = new List<ShenzhenCargo>();
            DataTable dt = cargoService.GetShenzhenCargoDeliveryNo(bagNumber);

            dt.AsEnumerable().ToList().ForEach(r =>
            {
                vm.List.Add(new ShenzhenCargo()
                {
                    TrackingNo = r.Field<string>("TrackingNo"),
                    DeliveryNo = r.Field<string>("DeliveryNo"),
                });
            });

            return PartialView(vm);
        }


        /// <summary>
        /// 批量貨況查詢明細表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchSearchCargo)]
        public ActionResult BatchSearchCargo2()
        {
            return View();
        }

        /// <summary>
        /// 批量貨況查詢明細表上傳
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchSearchCargo)]
        [HttpPost]
        public JsonResult BatchSearchCargo2(HttpPostedFileBase file)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            try
            {
                string fileType, fileName, filePath;
                if (file != null)
                {
                    if (file.ContentLength > 0)
                    {
                        fileType = Path.GetExtension(file.FileName);
                        if (fileType != ".xlsx")
                        {
                            resopnseModel.status = Status.error;
                            resopnseModel.msg = "副檔名需為xlsx";
                        }

                        if (resopnseModel.status != Status.error)
                        {
                            fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                            filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                            file.SaveAs(filePath);
                            //寫入資料
                            resopnseModel = cargoService.BatchSearchCargo2(filePath, fileName, Session["user_id"].ToString());
                        }
                    }
                }
                else
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "未選擇檔案";
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 批量貨況查詢明細表Excel
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchSearchCargo)]
        public ActionResult BatchSearchCargo2Excel(string upload_time, string upload_ope)
        {
            string fileName = "";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = GetBatchSearchCargo2Workbook(upload_time, upload_ope);
                fileName = $"{DateTime.Now.ToString("yyyyMMdd")}批量貨況查詢明細表.xlsx";
                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = msg }
            };
        }

        /// <summary>
        /// 批量貨況查詢蝦皮明細表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchSearchCargoShopee)]
        public ActionResult BatchSearchCargoShopee()
        {
            return View();
        }

        /// <summary>
        /// 批量貨況查詢蝦皮明細表上傳
        /// </summary>
        /// <param name="file"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchSearchCargoShopee)]
        [HttpPost]
        public JsonResult BatchSearchCargoShopee(HttpPostedFileBase file)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            try
            {
                string fileType, fileName, filePath;
                if (file != null)
                {
                    if (file.ContentLength > 0)
                    {
                        fileType = Path.GetExtension(file.FileName);
                        if (fileType != ".xlsx")
                        {
                            resopnseModel.status = Status.error;
                            resopnseModel.msg = "副檔名需為xlsx";
                        }

                        if (resopnseModel.status != Status.error)
                        {
                            fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                            filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                            file.SaveAs(filePath);
                            //寫入資料
                            resopnseModel = cargoService.BatchSearchCargo2(filePath, fileName, Session["user_id"].ToString());
                        }
                    }
                }
                else
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "未選擇檔案";
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 批量貨況查詢蝦皮明細表Excel
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.BatchSearchCargoShopee)]
        public ActionResult BatchSearchCargoShopeeExcel(string upload_time, string upload_ope)
        {
            string fileName = "";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = GetBatchSearchCargoShopeeWorkbook(upload_time, upload_ope);
                fileName = $"CNCB Return List-{DateTime.Now.ToString("yyyyMMdd")}.xlsx";
                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = msg }
            };
        }

        /// <summary>
        /// 批量貨況查詢明細表Workbook
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        IWorkbook GetBatchSearchCargo2Workbook(string upload_time, string upload_ope)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //取得批量貨況查詢明細表
            DataTable dt_Report = cargoService.GetBatchSearchCargo2(upload_time, upload_ope).dt;
            //產生EXCEL
            GetBatchSearchCargo2Sheet(workbook, dt_Report, "批量貨況查詢明細表");
            return workbook;
        }

        /// <summary>
        /// 批量貨況查詢明細表Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Report"></param>
        /// <param name="sheetName"></param>
        void GetBatchSearchCargo2Sheet(IWorkbook workbook, DataTable dt_Report, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("預計到港日");
            row.CreateCell(1).SetCellValue("倉儲類型");
            row.CreateCell(2).SetCellValue("客戶名稱");
            row.CreateCell(3).SetCellValue("主提單號");
            row.CreateCell(4).SetCellValue("清關袋號");
            row.CreateCell(5).SetCellValue("分提單號");
            row.CreateCell(6).SetCellValue("進倉時間");
            row.CreateCell(7).SetCellValue("出倉時間");
            row.CreateCell(8).SetCellValue("掃貨上車");
            row.CreateCell(9).SetCellValue("接駁公司");
            row.CreateCell(10).SetCellValue("拆袋狀態");
            row.CreateCell(11).SetCellValue("派件公司");
            row.CreateCell(12).SetCellValue("派件公司(新)");
            row.CreateCell(13).SetCellValue("物流貨號");
            row.CreateCell(14).SetCellValue("收件人名稱");
            row.CreateCell(15).SetCellValue("收件人電話");
            row.CreateCell(16).SetCellValue("作業時間");
            row.CreateCell(17).SetCellValue("配送進度(最新的)");
            row.CreateCell(18).SetCellValue("客戶外箱號");
            row.CreateCell(19).SetCellValue("客戶訂單號");
            row.CreateCell(20).SetCellValue("尾程單號");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;
            row.GetCell(15).CellStyle = cs_Center;
            row.GetCell(16).CellStyle = cs_Center;
            row.GetCell(17).CellStyle = cs_Center;
            row.GetCell(18).CellStyle = cs_Center;
            row.GetCell(19).CellStyle = cs_Center;
            row.GetCell(20).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);
            sheet.SetColumnWidth(6, 5000);
            sheet.SetColumnWidth(7, 5000);
            sheet.SetColumnWidth(8, 5000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);
            sheet.SetColumnWidth(11, 5000);
            sheet.SetColumnWidth(12, 5000);
            sheet.SetColumnWidth(13, 5000);
            sheet.SetColumnWidth(14, 5000);
            sheet.SetColumnWidth(15, 5000);
            sheet.SetColumnWidth(16, 5000);
            sheet.SetColumnWidth(17, 15000);
            sheet.SetColumnWidth(18, 6000);
            sheet.SetColumnWidth(19, 6000);
            sheet.SetColumnWidth(20, 5000);

            DateTime eta, sign_in_time, trans_modify_time;
            for (int i = 0; i < dt_Report.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                if (DateTime.TryParse(dt_Report.Rows[i]["ETA"].ToString(), out eta))
                {
                    row.CreateCell(0).SetCellValue(eta);
                    row.GetCell(0).CellStyle = date2Style;
                }
                else
                {
                    row.CreateCell(0).SetCellValue(dt_Report.Rows[i]["ETA"].ToString());
                }
                row.CreateCell(1).SetCellValue(dt_Report.Rows[i]["I_DATA_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dt_Report.Rows[i]["CUSTOMER"].ToString());
                row.CreateCell(3).SetCellValue(dt_Report.Rows[i]["MAINNUMBER"].ToString());
                row.CreateCell(4).SetCellValue(dt_Report.Rows[i]["BL_NO"].ToString());
                row.CreateCell(5).SetCellValue(dt_Report.Rows[i]["TrackingNo"].ToString());
                row.CreateCell(6).SetCellValue(dt_Report.Rows[i]["I_SIGN_IN_TIME"].ToString());
                if (DateTime.TryParse(dt_Report.Rows[i]["I_SIGN_IN_TIME"].ToString(), out sign_in_time))
                {
                    row.CreateCell(6).SetCellValue(sign_in_time);
                    row.GetCell(6).CellStyle = dateStyle;
                }
                else
                {
                    row.CreateCell(6).SetCellValue(dt_Report.Rows[i]["I_SIGN_IN_TIME"].ToString());
                }
                if (DateTime.TryParse(dt_Report.Rows[i]["I_SIGN_OUT_TIME"].ToString(), out var sign_out_time))
                {
                    row.CreateCell(7).SetCellValue(sign_out_time);
                    row.GetCell(7).CellStyle = dateStyle;
                }
                else
                {
                    row.CreateCell(7).SetCellValue(dt_Report.Rows[i]["I_SIGN_OUT_TIME"].ToString());
                }

                if (DateTime.TryParse(dt_Report.Rows[i]["CargoUploadTime"].ToString(), out var cargoUploadTime))
                {
                    row.CreateCell(8).SetCellValue(cargoUploadTime);
                    row.GetCell(8).CellStyle = dateStyle;
                }
                else
                {
                    row.CreateCell(8).SetCellValue(dt_Report.Rows[i]["CargoUploadTime"].ToString());
                }

                row.CreateCell(9).SetCellValue(dt_Report.Rows[i]["PdtTransName"].ToString());

                if (int.TryParse(dt_Report.Rows[i]["SignOutTimeCount"].ToString(), out var signOutTimeCount))
                {
                    row.CreateCell(10).SetCellValue(signOutTimeCount == 1 ? "未拆" : "有拆");
                }
                else
                {
                    row.CreateCell(10).SetCellValue("未拆");
                }

                row.CreateCell(11).SetCellValue(dt_Report.Rows[i]["TRANS_NAME"].ToString());
                row.CreateCell(12).SetCellValue(dt_Report.Rows[i]["TRANS_NAME_NEW"].ToString());
                row.CreateCell(13).SetCellValue(dt_Report.Rows[i]["DELIVERYNO"].ToString());
                row.CreateCell(14).SetCellValue(dt_Report.Rows[i]["IMPORTER"].ToString());
                row.CreateCell(15).SetCellValue(dt_Report.Rows[i]["IM_PHONENO"].ToString());
                row.CreateCell(16).SetCellValue(dt_Report.Rows[i]["TRANS_MODIFY_TIME"].ToString());
                if (DateTime.TryParse(dt_Report.Rows[i]["TRANS_MODIFY_TIME"].ToString(), out trans_modify_time))
                {
                    row.CreateCell(16).SetCellValue(trans_modify_time);
                    row.GetCell(16).CellStyle = dateStyle;
                }
                else
                {
                    row.CreateCell(16).SetCellValue(dt_Report.Rows[i]["TRANS_MODIFY_TIME"].ToString());
                }
                row.CreateCell(17).SetCellValue(dt_Report.Rows[i]["TRANS_STATUS_DESC"].ToString());
                row.CreateCell(18).SetCellValue(dt_Report.Rows[i]["FIELD_X"].ToString());
                row.CreateCell(19).SetCellValue(dt_Report.Rows[i]["ORDER_NO"].ToString());
                row.CreateCell(20).SetCellValue(dt_Report.Rows[i]["EXPRESS_NO"].ToString());
            }
        }

        /// <summary>
        /// 批量貨況查詢蝦皮明細表Workbook
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        IWorkbook GetBatchSearchCargoShopeeWorkbook(string upload_time, string upload_ope)
        {
            IWorkbook workbook = new XSSFWorkbook();
            //取得批量貨況查詢明細表
            DataTable dt_Report = cargoService.GetBatchSearchCargo2(upload_time, upload_ope).dt;
            //產生EXCEL
            GetBatchSearchCargoShopeeSheet(workbook, dt_Report, "Details");
            return workbook;
        }

        /// <summary>
        /// 批量貨況查詢蝦皮明細表Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="dt_Report"></param>
        /// <param name="sheetName"></param>
        void GetBatchSearchCargoShopeeSheet(IWorkbook workbook, DataTable dt_Report, string sheetName)
        {
            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            ISheet sheet = workbook.CreateSheet(sheetName);
            //表頭 
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("Region");
            row.CreateCell(1).SetCellValue("shopee_batch");
            //尾程單號
            row.CreateCell(2).SetCellValue("lm_tracking_no");
            //分提單號
            row.CreateCell(3).SetCellValue("ordersn");
            //客戶訂單號
            row.CreateCell(4).SetCellValue("sls_trace_no");
            row.CreateCell(5).SetCellValue("Return_type");
            row.CreateCell(6).SetCellValue("Cargo_ready_date");
            row.CreateCell(7).SetCellValue("return_sls_trace_no");
            row.CreateCell(8).SetCellValue("Return_lm_tracking_no");
            row.CreateCell(9).SetCellValue("Carton_number");
            row.CreateCell(10).SetCellValue("Width_cm");
            row.CreateCell(11).SetCellValue("Length_cm");
            row.CreateCell(12).SetCellValue("Height_cm");
            row.CreateCell(13).SetCellValue("cbm");
            row.CreateCell(14).SetCellValue("weight");

            row.GetCell(0).CellStyle = cs_Center;
            row.GetCell(1).CellStyle = cs_Center;
            row.GetCell(2).CellStyle = cs_Center;
            row.GetCell(3).CellStyle = cs_Center;
            row.GetCell(4).CellStyle = cs_Center;
            row.GetCell(5).CellStyle = cs_Center;
            row.GetCell(6).CellStyle = cs_Center;
            row.GetCell(7).CellStyle = cs_Center;
            row.GetCell(8).CellStyle = cs_Center;
            row.GetCell(9).CellStyle = cs_Center;
            row.GetCell(10).CellStyle = cs_Center;
            row.GetCell(11).CellStyle = cs_Center;
            row.GetCell(12).CellStyle = cs_Center;
            row.GetCell(13).CellStyle = cs_Center;
            row.GetCell(14).CellStyle = cs_Center;

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);
            sheet.SetColumnWidth(5, 5000);
            sheet.SetColumnWidth(6, 5000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 5000);
            sheet.SetColumnWidth(10, 5000);
            sheet.SetColumnWidth(11, 5000);
            sheet.SetColumnWidth(12, 5000);
            sheet.SetColumnWidth(13, 5000);
            sheet.SetColumnWidth(14, 5000);

            string expressNo;
            for (int i = 0; i < dt_Report.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                expressNo = dt_Report.Rows[i]["EXPRESS_NO"].ToString().Trim();

                row.CreateCell(0).SetCellValue("TW");
                row.CreateCell(2).SetCellValue(expressNo != "" ? expressNo : "-");
                row.CreateCell(3).SetCellValue(dt_Report.Rows[i]["TrackingNo"].ToString());
                row.CreateCell(4).SetCellValue(dt_Report.Rows[i]["ORDER_NO"].ToString());
                row.CreateCell(5).SetCellValue("FD");
                row.CreateCell(7).SetCellValue("-");
                row.CreateCell(8).SetCellValue("-");
                row.CreateCell(9).SetCellValue("-");
                row.CreateCell(10).SetCellValue("-");
                row.CreateCell(11).SetCellValue("-");
                row.CreateCell(12).SetCellValue("-");
                row.CreateCell(13).SetCellValue("-");
                row.CreateCell(14).SetCellValue("-");
            }
        }


        /// <summary>
        /// 處置說明下載
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.DownloadProcess)]
        public ActionResult DownloadProcess()
        {
            string custId, custName;
            var vm = new DownloadProcessViewModel();
            vm.sDate = DateTime.Now.ToString("yyyy-MM-dd");
            vm.eDate = DateTime.Now.ToString("yyyy-MM-dd");
            //客戶
            var dt_CustList = customerService.GetCustomerList();

            var customerList = new List<SelectListItem>();
            customerList.Add(new SelectListItem() { Text = "全部", Value = "All" });
            for (int i = 0; i < dt_CustList.Rows.Count; i++)
            {
                custId = dt_CustList.Rows[i]["CUST_ID"].ToString().Trim();
                custName = $"{dt_CustList.Rows[i]["TRAN_TYPE"].ToString()}-{dt_CustList.Rows[i]["CUSTOMER"].ToString()}";
                customerList.Add(new SelectListItem() { Text = custName, Value = custId });
            }
            vm.ddlCustomerList = customerList;

            //分類
            var typeList = new List<SelectListItem>();
            typeList.Add(new SelectListItem() { Text = "全部", Value = "All" });
            typeList.Add(new SelectListItem() { Text = "貨況", Value = "1" });
            typeList.Add(new SelectListItem() { Text = "退運", Value = "2" });
            typeList.Add(new SelectListItem() { Text = "錯單公司名義收單", Value = "3" });
            typeList.Add(new SelectListItem() { Text = "現場轉出", Value = "4" });
            typeList.Add(new SelectListItem() { Text = "公司名義不回艙", Value = "5" });

            //分類
            var finistList = new List<SelectListItem>();
            finistList.Add(new SelectListItem() { Text = "全部", Value = "All" });
            finistList.Add(new SelectListItem() { Text = "結案", Value = "Y" });
            finistList.Add(new SelectListItem() { Text = "未結案", Value = "N" });

            vm.ddlProcessTypeList = typeList;
            vm.ddlFinistList = finistList;
            return View(vm);
        }

        /// <summary>
        ///  處置說明下載 Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.DownloadProcess)]
        public ActionResult ProcessExcel(DownloadProcessViewModel vm)
        {
            var sDate = Convert.ToDateTime(vm.sDate).ToString("yyyyMMdd");
            var eDate = Convert.ToDateTime(vm.eDate).ToString("yyyyMMdd");
            var dataTableModel = cargoService.ProcessReport(vm.custId, sDate, eDate, vm.ProcessType,vm.Finish);
            if (dataTableModel.status == Status.error)
                return new JsonResult() { Data = new { msg = dataTableModel.msg } };

            var dt = dataTableModel.dt;

            IWorkbook workbook = GetProcessWorkbook(dt);

            string handle = Guid.NewGuid().ToString();
            string fileName = "";

            fileName = $"{sDate}~{eDate}-處置說明.xlsx";

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = dataTableModel.msg }
            };
        }

        /// <summary>
        /// 處置說明 Excel格式
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public IWorkbook GetProcessWorkbook(DataTable dt)
        {
            IWorkbook workbook = new XSSFWorkbook();

            //取得EXCEL格式
            GetWorkbookStyle(workbook);

            XSSFDataFormat format = (XSSFDataFormat)workbook.CreateDataFormat();

            ISheet sheet = workbook.CreateSheet("報表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("輸入日期");
            row.CreateCell(1).SetCellValue("倉儲");
            row.CreateCell(2).SetCellValue("客戶名稱");
            row.CreateCell(3).SetCellValue("到港日");
            row.CreateCell(4).SetCellValue("主提單號");
            row.CreateCell(5).SetCellValue("清關袋號");
            row.CreateCell(6).SetCellValue("物流貨號");
            row.CreateCell(7).SetCellValue("入倉日期");
            row.CreateCell(8).SetCellValue("出倉日期");
            row.CreateCell(9).SetCellValue("收件人名稱");
            row.CreateCell(10).SetCellValue("收件人電話");
            row.CreateCell(11).SetCellValue("處置說明");
            row.CreateCell(12).SetCellValue("輸入時間");
            row.CreateCell(13).SetCellValue("輸入人員姓名");
            row.CreateCell(14).SetCellValue("處置說明結案碼");
            row.CreateCell(15).SetCellValue("輸入結案碼時間");
            row.CreateCell(16).SetCellValue("輸入結案碼人員姓名");


            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 3000);
            sheet.SetColumnWidth(2, 8000);
            sheet.SetColumnWidth(3, 4000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 8000);
            sheet.SetColumnWidth(10, 6000);
            sheet.SetColumnWidth(11, 30000);
            sheet.SetColumnWidth(12, 6000);
            sheet.SetColumnWidth(13, 6000);
            sheet.SetColumnWidth(14, 6000);
            sheet.SetColumnWidth(15, 6000);
            sheet.SetColumnWidth(16, 6000);


            DateTime eta, sign_in_time, sign_out_time, finish_datetime;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);

                row.CreateCell(0).SetCellValue(dt.Rows[i]["DATADATE"].ToString());
                row.CreateCell(1).SetCellValue(dt.Rows[i]["I_DATA_TYPE"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["CUSTOMER"].ToString());
                if (DateTime.TryParse(dt.Rows[i]["ETA"].ToString(), out eta))
                {
                    row.CreateCell(3).SetCellValue(eta);
                    row.GetCell(3).CellStyle = date2Style;
                }
                else
                {
                    row.CreateCell(3).SetCellValue(dt.Rows[i]["ETA"].ToString());
                }

                row.CreateCell(4).SetCellValue(dt.Rows[i]["MAINNUMBER"].ToString());
                row.CreateCell(5).SetCellValue(dt.Rows[i]["BL_NO"].ToString());
                row.CreateCell(6).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                if (DateTime.TryParse(dt.Rows[i]["I_SIGN_IN_TIME"].ToString(), out sign_in_time))
                {
                    row.CreateCell(7).SetCellValue(sign_in_time);
                    row.GetCell(7).CellStyle = dateStyle;
                }
                if (DateTime.TryParse(dt.Rows[i]["I_SIGN_OUT_TIME"].ToString(), out sign_out_time))
                {
                    row.CreateCell(8).SetCellValue(sign_out_time);
                    row.GetCell(8).CellStyle = dateStyle;
                }
                row.CreateCell(9).SetCellValue(dt.Rows[i]["IMPORTER"].ToString());
                row.CreateCell(10).SetCellValue(dt.Rows[i]["IM_PHONENO"].ToString());
                row.CreateCell(11).SetCellValue(dt.Rows[i]["REMARK"].ToString());
                row.CreateCell(12).SetCellValue(Convert.ToDateTime(dt.Rows[i]["CRTDATETIME"]));
                row.GetCell(12).CellStyle = dateStyle;
                row.CreateCell(13).SetCellValue(dt.Rows[i]["USER_NAME"].ToString());
                row.CreateCell(14).SetCellValue(dt.Rows[i]["FINISH"].ToString());
                if (DateTime.TryParse(dt.Rows[i]["FINISH_DATETIME"].ToString(), out finish_datetime))
                {
                    row.CreateCell(15).SetCellValue(finish_datetime);
                    row.GetCell(15).CellStyle = dateStyle;
                }
                row.CreateCell(16).SetCellValue(dt.Rows[i]["FINISH_USER_NAME"].ToString());
            }

            return workbook;
        }

        /// <summary>
        /// 轉檔查詢
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.SearchWork)]
        public ActionResult SearchWork()
        {
            return View();
        }

        /// <summary>
        /// 轉檔查詢-明細
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1")]
        [UserAuthorize(Authority.SearchWork)]
        public ActionResult GetLogWork()
        {
            DataTable dt = cargoService.GetLog_Work();
            string data = JsonConvert.SerializeObject(dt);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Excel Style
        /// </summary>
        /// <param name="workbook"></param>
        void GetWorkbookStyle(IWorkbook workbook)
        {
            //藍色的Style
            fontB = workbook.CreateFont();
            fontB.Color = NPOI.SS.UserModel.IndexedColors.Blue.Index;

            font1 = (XSSFFont)workbook.CreateFont();
            //標題
            cs_Title = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Title.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //標題
            cs_Title_Left = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Title_Left.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
            cs_Title_Left.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Title.BorderTop = BorderStyle.Thin;
            //cs_Title.BorderBottom = BorderStyle.Thin;
            //cs_Title.BorderLeft = BorderStyle.Thin;
            //cs_Title.BorderRight = BorderStyle.Thin;
            //cs_Title.SetFont(font1);

            cs_Center = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center.WrapText = true;//設置換行這個要先設置
            cs_Center.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Center.BorderTop = BorderStyle.Thin;
            //cs_Center.BorderBottom = BorderStyle.Thin;
            //cs_Center.BorderLeft = BorderStyle.Thin;
            //cs_Center.BorderRight = BorderStyle.Thin;

            cs_Center_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            cs_Center_Blue.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            cs_Center_Blue.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            //cs_Center_Blue.BorderTop = BorderStyle.Thin;
            //cs_Center_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Center_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Center_Blue.BorderRight = BorderStyle.Thin;
            cs_Center_Blue.SetFont(fontB);

            format = (XSSFDataFormat)workbook.CreateDataFormat();
            cs_Int = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int.BorderTop = BorderStyle.Thin;
            //cs_Int.BorderBottom = BorderStyle.Thin;
            //cs_Int.BorderLeft = BorderStyle.Thin;
            //cs_Int.BorderRight = BorderStyle.Thin;
            cs_Int.DataFormat = format.GetFormat("#,##0");

            cs_Int_Blue = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Int_Blue.BorderTop = BorderStyle.Thin;
            //cs_Int_Blue.BorderBottom = BorderStyle.Thin;
            //cs_Int_Blue.BorderLeft = BorderStyle.Thin;
            //cs_Int_Blue.BorderRight = BorderStyle.Thin;
            cs_Int_Blue.DataFormat = format.GetFormat("#,##0");
            cs_Int_Blue.SetFont(fontB);

            cs_Double = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Double.BorderTop = BorderStyle.Thin;
            //cs_Double.BorderBottom = BorderStyle.Thin;
            //cs_Double.BorderLeft = BorderStyle.Thin;
            //cs_Double.BorderRight = BorderStyle.Thin;
            cs_Double.DataFormat = format.GetFormat("#,##0.000");

            cs_Percent2 = (XSSFCellStyle)workbook.CreateCellStyle();
            //cs_Percent.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
            //cs_Percent.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
            cs_Percent2.DataFormat = format.GetFormat("0.000%");
            cs_Percent2.SetFont(font1);


            dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            dateStyle.DataFormat = format.GetFormat("yyyy/mm/dd hh:mm:ss");

            date2Style = (XSSFCellStyle)workbook.CreateCellStyle();
            date2Style.DataFormat = format.GetFormat("yyyy/mm/dd");

        }
    }
}