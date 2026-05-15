using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Service.Models;
using Service.Models.HctEtlTax;

namespace Service.Services.HctEtlTax
{
    public class HctEtlTaxService : _BaseService
    {
        public HctEtlTaxService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public IWorkbook GetWorkbook(string custCode,string startDate, string endDate)
        {
            var list = GetData(custCode, startDate, endDate);

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("明細表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("序號");
            row.CreateCell(1).SetCellValue("物流貨號");
            row.CreateCell(2).SetCellValue("訂單號");
            row.CreateCell(3).SetCellValue("收件人姓名");
            row.CreateCell(4).SetCellValue("收件人地址");
            row.CreateCell(5).SetCellValue("收件人電話");
            row.CreateCell(6).SetCellValue("託運備註");
            row.CreateCell(7).SetCellValue("商品別編號");
            row.CreateCell(8).SetCellValue("商品數量");
            row.CreateCell(9).SetCellValue("才積/重量/總長");
            row.CreateCell(10).SetCellValue("代收貨款");
            row.CreateCell(11).SetCellValue("指定配送日期");
            row.CreateCell(12).SetCellValue("指定配送時間");
            row.CreateCell(13).SetCellValue("派件公司");

            for (int i = 0; i < 14; i++)
            {
                sheet.SetColumnWidth(i, 5000);
            }
            

            var irow = 1;
            foreach (var item in list)
            {
                row = sheet.CreateRow(irow);
                row.CreateCell(0).SetCellValue(irow);
                row.CreateCell(1).SetCellValue(item.DeliveryNo);
                row.CreateCell(2).SetCellValue(item.TrackingNo);
                row.CreateCell(3).SetCellValue(item.Recipient);
                row.CreateCell(4).SetCellValue(item.RecAddress);
                row.CreateCell(5).SetCellValue(item.RecPhone);
                row.CreateCell(6).SetCellValue(item.Remark);
                row.CreateCell(8).SetCellValue(item.Quantity);
                row.CreateCell(9).SetCellValue(item.CeilingWeight);
                row.CreateCell(10).SetCellValue(item.To_Dlv_Cod);
                row.CreateCell(13).SetCellValue(item.Trans_Name);

                irow++;
            }

            return workbook;
        }

        List<HctEtlTaxDetail> GetData(string custCode, string startDate, string endDate)
        {
            var sql = @"
                        select a.MAIN_NUMBER,a.BAG_NUMBER,b.TRACKINGNO,
                        b.RECIPIENT,b.RECPHONE,b.RECADDRESS,b.REMARK,b.QUANTITY,b.WEIGHT,b.DELIVERYNO,b.DESPATCHNO,
                        c.TO_DLV_COD,
                        d.TRANS_NAME,d.COMPANY
                        from DATA_CENTER.dbo.CLEARANCE_INFO a
                        left join DATA_CENTER.dbo.ORIGINALLIST b on a.MAIN_NUMBER=b.MAINNUMBER and a.MERGE_NUMBER=b.TRACKINGNO
                        left join jetf.dbo.FEE_MASTER c on b.DELIVERYNO = c.DLV_INV
                        left join [dbo].[View_EtlCustomerTrans] d on b.TRANS_TAXPAYMENT = d.TRANS_NO
                        where a.SIGN_OUT_TIME between @startDate and @endDate
                        and DATA_TYPE in ('tact','ftz') and DESPATCHNO = @custCode 
                      ";

            var query = conn.Query<HctEtlTaxDetail>(sql, new { custCode, startDate, endDate }).ToList();

            //var result = query.Where(r => r.Company == "新竹物流" || r.Company == null)
            //    .ToList();

            return query;

        }
    }
}
