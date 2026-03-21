using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models.TpctContainer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Services
{
    public class TpctContainerService
    {
        SqlConnection conn;

        public TpctContainerService()
        {
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 查詢資料
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public IWorkbook Download(List<string> search)
        {
            IWorkbook workbook = new XSSFWorkbook();

            //查詢
            List<TpctContainerModel> list = SearcTpctContainer(search);

            //取得頁籤
            GetDetailSheet(workbook, list);

            return workbook;
        }

        /// <summary>
        /// 明細Sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="mawbList"></param>
        /// <returns></returns>
        void GetDetailSheet(IWorkbook workbook, List<TpctContainerModel> list)
        {
            var column = 0;
            ISheet sheet = workbook.CreateSheet("TPCT貨櫃動態");
            //表頭
            IRow row = sheet.CreateRow(0);
            row.CreateCell(column++).SetCellValue("ContainerNo");
            row.CreateCell(column++).SetCellValue("Date");
            row.CreateCell(column++).SetCellValue("Time");
            row.CreateCell(column++).SetCellValue("ContainerMovesDescription");
            row.CreateCell(column++).SetCellValue("VesselVoyage");
            row.CreateCell(column++).SetCellValue("Company");
            row.CreateCell(column++).SetCellValue("查詢結果");

            sheet.SetColumnWidth(0, 6000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 10000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 8000);
            sheet.SetColumnWidth(6, 10000);

            int iRow = 1;
            list.ForEach(r =>
            {
                column = 0;
                row = sheet.CreateRow(iRow);
                row.CreateCell(column++).SetCellValue(r.ContainerNo);
                row.CreateCell(column++).SetCellValue(r.Date);
                row.CreateCell(column++).SetCellValue(r.Time);
                row.CreateCell(column++).SetCellValue(r.ContainerMovesDescription);
                row.CreateCell(column++).SetCellValue(r.VesselVoyage);
                row.CreateCell(column++).SetCellValue(r.Company);
                row.CreateCell(column++).SetCellValue(r.Msg);
                iRow++;
            });
        }


        /// <summary>
        /// 查詢
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        List<TpctContainerModel> SearcTpctContainer(List<string> search)
        {
            List<TpctContainerModel> list = new List<TpctContainerModel>();

            foreach (var item in search)
            {
                var parameters = new Dictionary<string, string>
                {
                    { "PI_CNTR_NO",item }
                };

                var result = PostTpctContainer(parameters);

                if (result.success)
                {
                    result.data.ForEach(r =>
                    {
                        list.Add(new TpctContainerModel()
                        {
                            ContainerNo = item,
                            Date = r.opDate,
                            Time = r.opTime,
                            ContainerMovesDescription = r.status,
                            VesselVoyage = r.vslvoy,
                            Company = r.carrier,
                        });
                    });
                }
                else
                {
                    list.Add(new TpctContainerModel() 
                    { 
                        ContainerNo = item, 
                        Msg = result.msg 
                    });
                }

                Thread.Sleep(4000);
            }

            return list;
        }

        /// <summary>
        /// 主號
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        QueryCntrStatusModel PostTpctContainer(Dictionary<string, string> parameters)
        {
            string url = "https://service.tpct.com.tw/services/itc2/queryCntrStatus";
            QueryCntrStatusModel result = new QueryCntrStatusModel();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
                    //var formData = new FormUrlEncodedContent(parameters).ReadAsStringAsync().Result;
                    //var content = new StringContent(formData, Encoding.UTF8, "application/json;charset=UTF-8");


                    // 將字典轉換為表單資料
                    var formData = new FormUrlEncodedContent(parameters);

                    // 發送 POST 請求
                    HttpResponseMessage response = client.PostAsync(url, formData).Result;

                    // 檢查請求是否成功
                    if (response.IsSuccessStatusCode)
                    {
                        result = JsonConvert.DeserializeObject<QueryCntrStatusModel>(response.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        result.msg = "查詢失敗";
                    }
                }
            }
            catch (Exception ex)
            {
                result.msg = ex.Message;
            }

            return result;
        }
    }
}
