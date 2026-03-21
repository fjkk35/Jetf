using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CainiaoFamilyTax
{
    public class CainiaoFamilyTaxService : _BaseService
    {
        /// <summary>
        /// 取得全家稅金
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public IWorkbook GetCainiaoFamilyTax(string startDate, string endDate, EtlFamilyTax customer)
        {
            //取得派件
            var trans = string.Join(",", customer.GetTransValue()
                                .Split(',')
                                .Select(r => $"'{r}'")
                                .ToArray());

            string sql = $@"
                            select a.DLV_INV,a.TO_DLV_COD,b.ECM as TRACKINGNO from jetf.[dbo].[FEE_MASTER] a
                            left join DATA_CENTER.[dbo].ORIGINALLIST b on a.TRACKINGNO=b.TRACKINGNO
                            where DLV_COM in ({trans}) and a.INCLUDE_TAX='N' and OUT_DATETIME between @startDate and @endDate
                         ";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@startDate", SqlDbType.NVarChar).Value = startDate;
                da.SelectCommand.Parameters.Add("@endDate", SqlDbType.NVarChar).Value = endDate;
                da.Fill(dt);
            }

            return GetWorkbook(dt);
        }

        IWorkbook GetWorkbook(DataTable dt)
        {
            int to_dlv_cod;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("全家");
            //表頭  
            IRow row = sheet.CreateRow(0);
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
                if (int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out to_dlv_cod))
                {
                    row.CreateCell(2).SetCellValue(to_dlv_cod);
                }
            }

            return workbook;
        }
    }
}
