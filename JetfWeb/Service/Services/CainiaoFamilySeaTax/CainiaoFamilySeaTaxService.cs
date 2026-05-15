using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Service.Models.CainiaoFamilySeaTax;

namespace Service.Services.CainiaoFamilySeaTax
{
    public class CainiaoFamilySeaTaxService :_BaseService
    {
        public CainiaoFamilySeaTaxService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得全家稅金
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public IWorkbook GetCainiaoFamilySeaTax(string dataDate)
        {
            string sql = @"
                 select DLV_INV,TO_DLV_COD,ARRIVAL from jetf.[dbo].[FEE_MASTER] a
                 where DATADATE = @DATADATE and a.INCLUDE_TAX='N' and SOURCE_TYPE = '1' and Download='1' and
                 DLV_COM in ('菜鳥全家','菜鳥全家C','菜鳥全家P')  
                 union all
                 select DLV_INV,TO_DLV_COD,ARRIVAL from jetf.[dbo].[FEE_MASTER] a
                 where DATADATE = @DATADATE and a.INCLUDE_TAX='N' and SOURCE_TYPE = '2' and
                 DLV_COM in ('菜鳥全家','菜鳥全家C','菜鳥全家P')  
              ";


            var list = conn.Query<CainiaoFamilySeaTaxModel>(sql, new { DATADATE = dataDate }).ToList();
            return GetWorkbook(list);
        }

        IWorkbook GetWorkbook(List<CainiaoFamilySeaTaxModel> list)
        {
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

            foreach (var item in list)
            {
                row = sheet.CreateRow(list.IndexOf(item) + 1);
                row.CreateCell(0).SetCellValue(item.DLV_INV);
                row.CreateCell(1).SetCellValue(item.ARRIVAL);
                if (int.TryParse(item.TO_DLV_COD, out var to_dlv_cod))
                {
                    row.CreateCell(2).SetCellValue(to_dlv_cod);
                }
            }
            return workbook;
        }

    }
}
