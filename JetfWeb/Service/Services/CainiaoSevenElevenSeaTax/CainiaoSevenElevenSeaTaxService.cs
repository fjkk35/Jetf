using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CainiaoSevenElevenSeaTax
{
    public class CainiaoSevenElevenSeaTaxService : _BaseService
    {
        public CainiaoSevenElevenSeaTaxService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得7-11稅金
        /// </summary>
        /// <param name="dataDate"></param>
        /// <returns></returns>
        public IWorkbook GetCainiaoSevenElevenSeaTax(string dataDate)
        {
            string sql = @"
                            select DLV_INV,TO_DLV_COD from jetf.[dbo].[FEE_MASTER] a
                            where DATADATE = @DATADATE and a.INCLUDE_TAX='N' and SOURCE_TYPE = '1' and Download='1' and
                            DLV_COM in ('菜鳥711','菜鳥711C','菜鳥711P')  
                            union all
                            select DLV_INV,TO_DLV_COD from jetf.[dbo].[FEE_MASTER] a
                            where DATADATE = @DATADATE and a.INCLUDE_TAX='N' and SOURCE_TYPE = '2' and
                            DLV_COM in ('菜鳥711','菜鳥711C','菜鳥711P')
                         ";

            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@DATADATE", SqlDbType.NVarChar).Value = dataDate;
                da.Fill(dt);
            }

            var workbook = GetSeaWorkbook(dt);

            return workbook;
        }

        IWorkbook GetSeaWorkbook(DataTable dt)
        {
            int to_dlv_cod;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("7-11");
            //表頭  
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("母代號");
            row.CreateCell(1).SetCellValue("子代號");
            row.CreateCell(2).SetCellValue("配送編號");
            row.CreateCell(3).SetCellValue("服務類型");
            row.CreateCell(4).SetCellValue("出貨單金額");

            sheet.SetColumnWidth(0, 3000);
            sheet.SetColumnWidth(1, 3000);
            sheet.SetColumnWidth(2, 6000);
            sheet.SetColumnWidth(3, 3000);
            sheet.SetColumnWidth(4, 6000);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                row = sheet.CreateRow(i + 1);
                row.CreateCell(0).SetCellValue("74A");
                row.CreateCell(1).SetCellValue("002");
                row.CreateCell(2).SetCellValue(dt.Rows[i]["DLV_INV"].ToString());
                row.CreateCell(3).SetCellValue("1");
                if (int.TryParse(dt.Rows[i]["TO_DLV_COD"].ToString(), out to_dlv_cod))
                {
                    row.CreateCell(4).SetCellValue(to_dlv_cod);
                }
            }

            return workbook;
        }
    }
}
