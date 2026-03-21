using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CainiaoHiLifeTaxDetails
{
    public class CainiaoHiLifeTaxDetailsService :_BaseService
    {
        /// <summary>
        /// 取得萊爾富接收稅金明細表
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public DataTable GetData(string startDate,string endDate) 
        {
            string sql = @"
                           select UploadTime,DlvInv,Tax,ReplyTax,ReplyTime from [dbo].[CainiaoHiLifeTax] a
                           where UploadTime between @StartTime and @EndTime
                           and UploadTime = (select max(UploadTime) from [dbo].[CainiaoHiLifeTax] where DlvInv = a.DlvInv)
                         ";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@StartTime", SqlDbType.NVarChar).Value = startDate;
                da.SelectCommand.Parameters.Add("@EndTime", SqlDbType.NVarChar).Value = endDate;
                da.Fill(dt);
            }

            return dt;
        }

        public IWorkbook GetWorkbook(string startDate, string endDate) 
        {
            var dt = GetData(startDate, endDate);

            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("明細表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("捷豐上傳日期");
            row.CreateCell(1).SetCellValue("物流單號");
            row.CreateCell(2).SetCellValue("稅金金額");
            row.CreateCell(3).SetCellValue("萊爾富稅金金額");
            row.CreateCell(4).SetCellValue("萊爾富接收日期");

            sheet.SetColumnWidth(0, 5000);
            sheet.SetColumnWidth(1, 5000);
            sheet.SetColumnWidth(2, 5000);
            sheet.SetColumnWidth(3, 5000);
            sheet.SetColumnWidth(4, 5000);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(Convert.ToDateTime(dt.Rows[i]["UploadTime"]).ToString("yyyy/MM/dd"));
                row.CreateCell(1).SetCellValue(dt.Rows[i]["DlvInv"].ToString());
                if(int.TryParse(dt.Rows[i]["Tax"].ToString(),out var tax))
                    row.CreateCell(2).SetCellValue(tax);
                if (int.TryParse(dt.Rows[i]["ReplyTax"].ToString(), out var replyTax))
                    row.CreateCell(3).SetCellValue(replyTax);
                if (DateTime.TryParse(dt.Rows[i]["ReplyTime"].ToString(), out var replyTime))
                    row.CreateCell(4).SetCellValue(replyTax.ToString("yyyy/MM/dd"));
            }

            return workbook;
        }
    }
}
