using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CainiaoTaixinStarTax
{
    public class CainiaoTaixinStarTaxService : _BaseService
    {
        public CainiaoTaixinStarTaxService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public IWorkbook GetWorkbook(string startDate, string endDate)
        {
            var workbook = new XSSFWorkbook();

            //超峰
            var dt = GetTaixinStarTax(startDate, endDate, new string[] { "26", "26C","26P","107", "107C", "107P", "108", "108C", "108P" });

            GetSheet(workbook, "超峰", dt);

            return workbook;
        }

        ISheet GetSheet(XSSFWorkbook workbook,string sheetName, DataTable dt) 
        {
            var sheet = workbook.CreateSheet(sheetName);
            //表頭  
            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("物流單號");
            row.CreateCell(1).SetCellValue("菜鳥LP單號");
            row.CreateCell(2).SetCellValue("稅金");

            sheet.SetColumnWidth(0, 4500);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 3500);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                row.CreateCell(1).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                if (int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out var to_dlv_cod))
                {
                    row.CreateCell(2).SetCellValue(to_dlv_cod);
                }
            }

            return sheet;
        }


        DataTable GetTaixinStarTax(string startDate, string endDate, string[] dlvCom)
        {
            string sql = $@"
                            select a.DLV_INV,a.TO_DLV_COD,b.ECM as TRACKINGNO from jetf.[dbo].[FEE_MASTER] a
                            left join DATA_CENTER.[dbo].ORIGINALLIST b on a.TRACKINGNO=b.TRACKINGNO
                            where DLV_COM in ({string.Join(",", dlvCom.Select(r => $"'{ r }'"))}) and a.INCLUDE_TAX ='N' and OUT_DATETIME between @startDate and @endDate
                         ";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@startDate", SqlDbType.NVarChar).Value = startDate;
                da.SelectCommand.Parameters.Add("@endDate", SqlDbType.NVarChar).Value = endDate;
                da.Fill(dt);
            }

            return dt;
        }
    }
}
