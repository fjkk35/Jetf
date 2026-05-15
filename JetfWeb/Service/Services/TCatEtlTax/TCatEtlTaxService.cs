using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class TCatEtlTaxService :_BaseService
    {
        public TCatEtlTaxService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得稅金明細表
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public IWorkbook GetTCatEtlTax(string startDate, string endDate)
        {
            string sql = @"
                        select BAG_NUMBER,TRACKINGNO,TO_DLV_COD,RECIPIENT,RECPHONE,b.TRANS_NAME,OUT_DATETIME,a.INCLUDE_TAX,a.DLV_INV from jetf.dbo.FEE_MASTER a 
                        left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.customer,5)=b.CUST_ID and a.dlv_com=b.TRANS_NO and TRAN_TYPE='空運'
                        where [SOURCE] in('tact','ftz') and 
                        b.COMPANY = '黑貓' and OUT_DATETIME between @StartTime and @EndTime and 
                        a.INCLUDE_TAX in ('N')";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@StartTime", SqlDbType.NVarChar).Value = startDate;
                da.SelectCommand.Parameters.Add("@EndTime", SqlDbType.NVarChar).Value = endDate;
                da.Fill(dt);
            }

            return GetWorkbook(dt);
        }

        IWorkbook GetWorkbook(DataTable dt)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("黑貓");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("託運單號");
            row.CreateCell(1).SetCellValue("關稅金額");

            sheet.SetColumnWidth(0, 4500);
            sheet.SetColumnWidth(1, 4500);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                if (int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out var to_dlv_cod))
                {
                    row.CreateCell(1).SetCellValue(to_dlv_cod);
                }
            }

            sheet = workbook.CreateSheet("明細");
            //表頭  
            row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("清關袋號");
            row.CreateCell(2).SetCellValue("運單號");
            row.CreateCell(3).SetCellValue("稅金");
            row.CreateCell(4).SetCellValue("納稅義務人");
            row.CreateCell(5).SetCellValue("電話");
            row.CreateCell(6).SetCellValue("派件公司");
            row.CreateCell(7).SetCellValue("稅金類別");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out var to_dlv_cod);
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["BAG_NUMBER"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                row.CreateCell(3).SetCellValue(to_dlv_cod);
                row.CreateCell(4).SetCellValue(dt.Rows[i]["RECIPIENT"].ToString());
                row.CreateCell(5).SetCellValue(dt.Rows[i]["RECPHONE"].ToString());
                row.CreateCell(6).SetCellValue(dt.Rows[i]["TRANS_NAME"].ToString());
                row.CreateCell(7).SetCellValue("不包稅");
            }

            return workbook;
        }

    }
}
