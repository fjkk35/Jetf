using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.Models.CainiaoHiLifeTax;
using Service.Models;
using Service.Models.CptTradeVan;
using System.IO;
using Service.Models.SevenElevenEtlTax;
using Service.Extensions;
using Dapper;
using Renci.SshNet;
using FluentFTP;
using iTextSharp.text.pdf.parser;
using System.Net;
using System.Net.NetworkInformation;
using FluentFTP.Helpers;

namespace Service.Services.SevenElevenEtlTaxTax
{
    public class SevenElevenEtlTaxService : _BaseService
    {
        /// <summary>
        /// 取得7-11稅金
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="customer"></param>
        /// <returns></returns>
        public byte[] GetSevenElevenTax(string startDate, string endDate, EtlSevenElevenTax customer)
        {
            switch (customer)
            {
                case EtlSevenElevenTax.Sagawa:
                    return GetSagawaTax(startDate, endDate, customer);
                case EtlSevenElevenTax.Cainiao:
                    return GetCainiaoTax(startDate, endDate, customer);
                default:
                    return new byte[0];
            }
        }

        /// <summary>
        /// 取得佐川7-11稅金（TXT格式）
        /// </summary>
        private byte[] GetSagawaTax(string startDate, string endDate, EtlSevenElevenTax customer)
        {
            //取得派件
            var trans = string.Join(",", customer.GetTransValue()
                                .Split(',')
                                .Select(r => $"'{r}'")
                                .ToArray());

            string sql = $@"
                          select a.DataDate,a.DLV_INV,a.TRACKINGNO,a.TO_DLV_COD,b.STORE_ID from jetf.[dbo].[FEE_MASTER] a
                          left join (select STORE_ID,DELIVER_NO from DATA_CENTER.dbo.ETL_ORDER_INFO where CUST_CODE = 'CN00010') b on a.DLV_INV=b.DELIVER_NO
                          where DLV_COM in ({trans}) and a.INCLUDE_TAX='N' and OUT_DATETIME between @startDate and @endDate
                         ";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@startDate", SqlDbType.NVarChar).Value = startDate;
                da.SelectCommand.Parameters.Add("@endDate", SqlDbType.NVarChar).Value = endDate;
                da.Fill(dt);
            }

            return GetSagawaBytes(dt);
        }

        /// <summary>
        /// 取得菜鳥7-11稅金（Excel格式）
        /// </summary>
        private byte[] GetCainiaoTax(string startDate, string endDate, EtlSevenElevenTax customer)
        {
            var dlvComList = customer.GetTransValue().Split(',').Select(x => x).ToList();

            string sql = @"
                select DLV_INV, TO_DLV_COD 
                from jetf.[dbo].[FEE_MASTER]
                where INCLUDE_TAX='N' 
                and OUT_DATETIME between @startDate and @endDate
                and DLV_COM in @dlvComList
            ";

            var result = conn.Query<CainiaoSevenElevenEtlTaxModel>(sql, new
            {
                startDate,
                endDate,
                dlvComList
            }).ToList();

            return GetCainiaoExcel(result);
        }

        /// <summary>
        /// 產生佐川 TXT 檔案
        /// </summary>
        byte[] GetSagawaBytes(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();

            // 檢查當前時間是否超過 11 點
            var now = DateTime.Now;
            var date = now.Hour >= 11 ? now.AddDays(1).ToString("yyyyMMdd") : now.ToString("yyyyMMdd");

            foreach (DataRow item in dt.Rows)
            {
                //廠商代號
                var childCode = "016";
                //配送編號 
                var dlvInv = item["DLV_INV"].ToString().PadLeft(8, '0');
                //出貨日期
                var dataDate = date;
                //金額
                var toDlvCod = Convert.ToInt32(item["TO_DLV_COD"]).ToString().PadLeft(5, '0');
                //分提單號
                var trackingNo = item["TRACKINGNO"].ToString().PadLeft(30, ' ');
                //是否為最後一次出貨(Y:是 N:否)
                var lastShipment = "Y";
                //serviceType
                var serviceType = "1";
                //門市店號 
                var eshopNo = item["STORE_ID"].ToString().PadLeft(6, ' ');
                //eshopType
                var eshopType = "04";

                sb.AppendLine($"{childCode}{dlvInv}{dataDate}{toDlvCod}{trackingNo}{lastShipment}{serviceType}{eshopNo}{eshopType}{toDlvCod}");
            }

            // 將文字內容轉換成位元組
            byte[] fileBytes = Encoding.UTF8.GetBytes(sb.ToString());

            return fileBytes;
        }

        /// <summary>
        /// 產生菜鳥 Excel 檔案
        /// </summary>
        byte[] GetCainiaoExcel(List<CainiaoSevenElevenEtlTaxModel> modelList)
        {
            foreach (var model in modelList)
            {
                model.ParentCode = "74A";
                model.ChildCode = "002";
                model.ServiceType = "1";
            }

            using (MemoryStream ms = new MemoryStream())
            {
                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("菜鳥7-11稅金");

                ICellStyle headerStyle = NpoiStyle.CreateHeaderStyle(workbook);
                ICellStyle dataStyle = NpoiStyle.CreateDataStyle(workbook);

                IRow headerRow = sheet.CreateRow(0);
                string[] headers = { "母代號", "子代號", "配送編號", "服務類型", "出貨單金額" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                int rowIndex = 1;
                foreach (var model in modelList)
                {
                    IRow dataRow = sheet.CreateRow(rowIndex++);
                    dataRow.CreateCell(0).SetCellValue(model.ParentCode);
                    dataRow.CreateCell(1).SetCellValue(model.ChildCode);
                    dataRow.CreateCell(2).SetCellValue(model.DlvInv);
                    dataRow.CreateCell(3).SetCellValue(model.ServiceType);
                    dataRow.CreateCell(4).SetCellValue(model.ToDlvCod);

                    for (int i = 0; i < 5; i++)
                    {
                        dataRow.GetCell(i).CellStyle = dataStyle;
                    }
                }

                for (int i = 0; i < headers.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                }

                workbook.Write(ms);
                return ms.ToArray();
            }
        }

        public ResponseModel Upload(string filePath, string userId)
        {
            var fileName = $"74B016{DateTime.Now.ToString("yyyyMMdd")}01.sup";
            var resopnseModel = new ResponseModel();

            try
            {
                //讀取 Txt
                var modelList = ReadTxt(filePath);

                ////新增資料
                resopnseModel = InsertSagawaData(modelList, fileName, userId);

                //上傳FTP
                if (resopnseModel.status == Status.success)
                {
                    var reslult = UploadFtp(filePath, fileName);
                    if (!reslult)
                    {
                        resopnseModel = new ResponseModel("FTP上傳失敗");
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }

            return resopnseModel;
        }

        List<SagawaSevenElevenEtlTaxModel> ReadTxt(string filePath)
        {
            var list = new List<SagawaSevenElevenEtlTaxModel>();

            byte[] fileBytes = File.ReadAllBytes(filePath);
            string fileContent = Encoding.UTF8.GetString(fileBytes);
            using (var reader = new StringReader(fileContent))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var item = new SagawaSevenElevenEtlTaxModel
                    {
                        ChildCode = line.Substring(0, 3).Trim(),
                        DlvInv = line.Substring(3, 8).Trim(),
                        DataDate = line.Substring(11, 8).Trim(),
                        ToDlvCod = line.Substring(19, 5).Trim(),
                        OrderNo = line.Substring(24, 30).Trim(),
                        LastShipment = line.Substring(54, 1).Trim(),
                        ServiceType = line.Substring(55, 1).Trim(),
                        EshopNo = line.Substring(56, 6).Trim(),
                        EshopType = line.Substring(62, 2).Trim(),
                    };

                    list.Add(item);
                }
            }

            return list;
        }





        /// <summary>
        /// 新增佐川上傳7-11資料
        /// </summary>
        /// <param name="list"></param>
        /// <param name="fileName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private ResponseModel InsertSagawaData(List<SagawaSevenElevenEtlTaxModel> list, string fileName, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = $"上傳成功筆數：{list.Count}";

            string sql = @"
                            insert [jetf].[dbo].[SagawaSevenElevenEtlTax](
                                ChildCode, DlvInv, DataDate, ToDlvCod, OrderNo, LastShipment, ServiceType, EshopNo, EshopType, FileName, UploadOpe, UploadTime)
                            values (
                                @ChildCode, @DlvInv, @DataDate, @ToDlvCod, @OrderNo, @LastShipment, @ServiceType, @EshopNo, @EshopType, @FileName, @UploadOpe, @UploadTime)";

            var uploadTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            conn.Open();
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    foreach (var item in list)
                    {
                        conn.Execute(sql, new
                        {
                            ChildCode = item.ChildCode,
                            DlvInv = item.DlvInv,
                            DataDate = item.DataDate,
                            ToDlvCod = item.ToDlvCod,
                            OrderNo = item.OrderNo,
                            LastShipment = item.LastShipment,
                            ServiceType = item.ServiceType,
                            EshopNo = item.EshopNo,
                            EshopType = item.EshopType,
                            FileName = System.IO.Path.GetFileName(fileName),
                            UploadOpe = userId,
                            UploadTime = uploadTime,
                        }, transaction: tran);
                    }

                    // 確認寫入
                    tran.Commit();
                }
                catch (Exception ex)
                {
                    resopnseModel = new ResponseModel(ex.Message);

                    // 取消寫入
                    tran.Rollback();
                }
                finally
                {
                    conn.Close();
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 上傳FTP
        /// </summary>
        private bool UploadFtp(string localFilePath, string fileName)
        {
            using (FtpClient ftp = new FtpClient())
            {
                ftp.Host = "ftps1.presco.com.tw";
                ftp.Port = 990;
                ftp.Credentials = new NetworkCredential("74B016", "ihPu+ElOLWN2WmCFhD1GlY9e");
                ftp.Config.EncryptionMode = FtpEncryptionMode.Implicit;
                ftp.Connect();
                var remoteFilePath = $"/SUP/{fileName}";
                var ftpStatus = ftp.UploadFile(localFilePath, remoteFilePath);
                ftp.Disconnect();

                return ftpStatus.IsSuccess();
            }
        }
    }
}
