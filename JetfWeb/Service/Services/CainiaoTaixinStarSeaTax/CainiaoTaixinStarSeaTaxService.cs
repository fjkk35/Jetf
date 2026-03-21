using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CainiaoTaixinStarSeaTax
{
    public class CainiaoTaixinStarSeaTaxService : _BaseService
    {
        public XSSFWorkbook GetWorkbook(DateTime dataDate)
        {
            var dt = GetTax(dataDate);

            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("超峰黑貓");
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

            return workbook;
        }

        /// <summary>
        /// 取得超峰黑貓稅金
        /// </summary>
        /// <param name="dataDate"></param>
        /// <returns></returns>
        public DataTable GetTax(DateTime dataDate)
        {
            string sql = @"
                            select DLV_INV,TO_DLV_COD,b.ECM as TRACKINGNO from jetf.[dbo].[FEE_MASTER] a
                            left join DATA_CENTER.[dbo].ORIGINALLIST b on a.TRACKINGNO=b.TRACKINGNO
                            where DATADATE = @DATADATE and a.INCLUDE_TAX='N' and SOURCE_TYPE = '1' and Download='1' and
                            DLV_COM in (N'超峰黑貓',N'超峰黑貓C',N'超峰黑貓P')  
                            union all
                            select DLV_INV,TO_DLV_COD,b.ECM as TRACKINGNO from jetf.[dbo].[FEE_MASTER] a
                            left join DATA_CENTER.[dbo].ORIGINALLIST b on a.TRACKINGNO=b.TRACKINGNO
                            where DATADATE = @DATADATE and a.INCLUDE_TAX='N' and SOURCE_TYPE = '2' and
                            DLV_COM in (N'超峰黑貓',N'超峰黑貓C',N'超峰黑貓P') 
                         ";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate.ToString("yyyyMMdd");
                da.Fill(dt);
            }

            return dt;
        }
    }
}
