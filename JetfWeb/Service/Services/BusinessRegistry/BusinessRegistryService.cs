using Dapper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Models.BusinessRegistry;
using Service.Models.SeaUnreceivedOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.BatchUploadProcess
{
    public class BusinessRegistryService : _BaseService
    {
        public BusinessRegistryService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        public ResponseModel GetExecl(string businessId)
        {
            try
            {
                var list = GetList(businessId);

                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("sheet1");
                //表頭  
                IRow row = sheet.CreateRow(0);
                row.CreateCell(0).SetCellValue("統一編號");
                row.CreateCell(1).SetCellValue("營業人名稱");

                sheet.SetColumnWidth(0, 3000);
                sheet.SetColumnWidth(1, 10000);

                int iRow = 1;
                list.ForEach(r =>
                {
                    row = sheet.CreateRow(iRow);
                    row.CreateCell(0).SetCellValue(r.BusinessId);
                    row.CreateCell(1).SetCellValue(r.BusinessName);
                    iRow++;
                });
                return new ResponseModel() { ReturnObject = workbook };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        private List<BusinessRegistryModel> GetList(string businessId)
        {
            var businessIdList = businessId
               .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
               .Where(r => !string.IsNullOrWhiteSpace(r))
               .Select(r => r.Trim())
               .ToList();

            var sql = @"
                         declare @BusinessTable Table
                         ( 
	                           BusinessId char(8)
                         )
                          {0};

                         select a.BusinessId,b.BusinessName from @BusinessTable a
                         left join jetf.[dbo].[BusinessRegistry] b on a.BusinessId=b.BusinessId
                   ";

            sql = string.Format(sql, $@"INSERT INTO @BusinessTable VALUES {string.Join(",",
                businessIdList.Select(r => $"('{r}')"))};");


            return conn.Query<BusinessRegistryModel>(sql).ToList();
        }
    }
}
