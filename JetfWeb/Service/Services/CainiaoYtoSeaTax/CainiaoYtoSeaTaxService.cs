using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using System.Data.SqlClient;
using Service.Extensions;

namespace Service.Services.CainiaoYtoSeaTax
{
    public class CainiaoYtoSeaTaxService : _BaseService
    {
        private readonly GlobalService _globalService;

        public CainiaoYtoSeaTaxService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, GlobalService globalService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _globalService = globalService;
        }

        public IWorkbook GetCainiaoYtoSeaTax(string dataDate)
        {

            DataTable dt = GetData(dataDate);
           
            var workbook = GetSeaWorkbook(dt);

            return workbook;
        }

        DataTable GetData(string dataDate) {
            DataTable dt = new DataTable();
            string sql = @"
                            select b.CUST_NAME,a.TRACKINGNO,a.DLV_INV,a.TO_DLV_COD,a.RECIPIENT,a.RECPHONE,a.INCLUDE_TAX,a.COMBINE,a.TYPE,a.DLV_COM,a.FEE,a.TRANS_COD from jetf.dbo.FEE_MASTER a 
                            left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE 
                            where DATADATE=@DATADATE and a.INCLUDE_TAX ='N' and SOURCE_TYPE = '1' and Download='1' 
                            and a.DLV_COM in ('菜鳥圓通','菜鳥圓通C','菜鳥圓通P')
                            union all
                            select b.CUST_NAME,a.TRACKINGNO,a.DLV_INV,a.TO_DLV_COD,a.RECIPIENT,a.RECPHONE,a.INCLUDE_TAX,a.COMBINE,a.TYPE,a.DLV_COM,a.FEE,a.TRANS_COD from jetf.dbo.FEE_MASTER a
                            left join Data_center.dbo.sys_cust b on a.CUSTOMER = b.CUST_CODE
                            where DATADATE = @DATADATE and a.INCLUDE_TAX = 'N' and SOURCE_TYPE = '2'  
                            and DLV_COM in ('菜鳥圓通', '菜鳥圓通C', '菜鳥圓通P')
                         ";


            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                da.Fill(dt);
            }
            return dt;
        }

        IWorkbook GetSeaWorkbook(DataTable dt)
        {
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
            row.CreateCell(10).SetCellValue("手續費");
            row.CreateCell(11).SetCellValue("跟派件收");

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
            sheet.SetColumnWidth(10, 6000);
            sheet.SetColumnWidth(11, 6000);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue(i + 1);
                row.CreateCell(1).SetCellValue(dt.Rows[i]["CUST_NAME"].ToString());
                row.CreateCell(2).SetCellValue(dt.Rows[i]["TRACKINGNO"].ToString());
                row.CreateCell(3).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                row.CreateCell(4).SetCellValue(dt.Rows[i]["TO_DLV_COD"].ToString().ToInt());
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
                row.CreateCell(10).SetCellValue(dt.Rows[i]["FEE"].ToString().ToInt());
                row.CreateCell(11).SetCellValue(dt.Rows[i]["TRANS_COD"].ToString().ToInt());
            }

            return workbook;
        }
    }
}
