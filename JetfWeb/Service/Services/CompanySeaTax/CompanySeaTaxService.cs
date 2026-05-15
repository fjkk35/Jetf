using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static iTextSharp.text.pdf.PRTokeniser;
using Service.EnumTax;
using Dapper;

namespace Service.Services.CompanySeaTax
{
    public class CompanySeaTaxService : _BaseService
    {
        private readonly GlobalService _globalService;

        public CompanySeaTaxService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, GlobalService globalService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _globalService = globalService;
        }

        public IWorkbook GetWorkbook(string dataDate,string company, SeaTaxType taxType)
        {
            var workbook = new XSSFWorkbook();

            var dt = GetSeaTax(dataDate, company, taxType);

            return GetSeaWorkbook(dt);
        }

        IWorkbook GetSeaWorkbook(DataTable dt)
        {
            int to_dlv_cod;
            string remark;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("報表");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("項次");
            row.CreateCell(1).SetCellValue("客戶");
            row.CreateCell(2).SetCellValue("清關袋號");
            row.CreateCell(3).SetCellValue("運單號");
            row.CreateCell(4).SetCellValue("稅金");
            row.CreateCell(5).SetCellValue("納稅義務人");
            row.CreateCell(6).SetCellValue("電話");
            row.CreateCell(7).SetCellValue("備註");
            row.CreateCell(8).SetCellValue("派件公司");
            row.CreateCell(9).SetCellValue("稅金類別");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 6000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 6000);
            sheet.SetColumnWidth(4, 6000);
            sheet.SetColumnWidth(5, 6000);
            sheet.SetColumnWidth(6, 6000);
            sheet.SetColumnWidth(7, 6000);
            sheet.SetColumnWidth(8, 6000);
            sheet.SetColumnWidth(9, 6000);


            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["CUST_NAME"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                if (int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out to_dlv_cod))
                {
                    row.CreateCell(4).SetCellValue(to_dlv_cod);
                }
                row.CreateCell(5).SetCellValue(dt.Rows[i]["RECIPIENT"].ToString());
                row.CreateCell(6).SetCellValue(dt.Rows[i]["RECPHONE"].ToString());
                remark = "單";
                if (dt.Rows[i]["COMBINE"].ToString() == "Y")
                {
                    remark = "併單";
                }
                else if (dt.Rows[i]["TYPE"].ToString() == "G")
                {
                    remark = "G類";
                }
                row.CreateCell(7).SetCellValue(remark);
                row.CreateCell(8).SetCellValue(dt.Rows[i]["DLV_COM"].ToString());
                row.CreateCell(9).SetCellValue(_globalService.GetTaxType(dt.Rows[i]["INCLUDE_TAX"].ToString()));
            }

            return workbook;
        }

        DataTable GetSeaTax(string dataDate,string company, SeaTaxType taxType)
        {
            var dt =new DataTable();
            var sb = new StringBuilder();
            sb.Append(" select b.CUST_NAME,a.TRACKINGNO,a.DLV_INV,a.TO_DLV_COD,a.RECIPIENT,a.RECPHONE,a.INCLUDE_TAX,a.COMBINE,a.TYPE,a.DLV_COM from jetf.dbo.FEE_MASTER a ");
            sb.Append(" left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
            sb.Append(" left join jetf.dbo.customer_master c on a.CUSTOMER=c.CUST_ID and a.DLV_COM=c.TRANS_NAME and c.TRAN_TYPE='海運' ");
            sb.Append(" where DATADATE=@DataDate and a.INCLUDE_TAX = 'N' and ");
            sb.Append(" [SOURCE]=@Source and c.COMPANY=@Company and Download='1' ");

            using (var da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.SelectCommand.Parameters.Add("@DataDate", SqlDbType.NVarChar).Value = dataDate;
                da.SelectCommand.Parameters.Add("@Company", SqlDbType.NVarChar).Value = company;
                da.SelectCommand.Parameters.Add("@Source", SqlDbType.NVarChar).Value = taxType.ToString();
                da.Fill(dt);
            }

            return dt;
        }
    }
}
