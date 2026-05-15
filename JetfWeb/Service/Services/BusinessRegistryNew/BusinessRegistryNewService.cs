using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Service.Models.BusinessRegistryNew;
using CompanyRegistrationLibrary;

namespace Service.Services.BusinessRegistryNew
{
    public class BusinessRegistryNewService : _BaseService
    {
        private readonly CompanyRegistration _companyRegistration;

        public BusinessRegistryNewService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext) 
            : base(jetfDbContext, dataCenterDbContext)
        {
            _companyRegistration = new CompanyRegistration();        }

        public async Task<ResponseModel> Search(string businessId)
        {
            var businessIds = businessId
           .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
           .Where(r => !string.IsNullOrWhiteSpace(r))
           .Select(r => r.Trim())
           .ToList();

            var list = await GetList(businessIds);

            return new ResponseModel() { ReturnObject = list };
        }

        public async Task<ResponseModel> GetExecl(string businessId)
        {
            try
            {
                var businessIds = businessId
                       .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                       .Where(r => !string.IsNullOrWhiteSpace(r))
                       .Select(r => r.Trim())
                       .ToList();

                var list = await GetList(businessIds);

                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("sheet1");
                //表頭  
                IRow row = sheet.CreateRow(0);
                row.CreateCell(0).SetCellValue("統一編號");
                row.CreateCell(1).SetCellValue("營業人名稱");
                row.CreateCell(2).SetCellValue("狀態");
                row.CreateCell(3).SetCellValue("核准解散日期");

                sheet.SetColumnWidth(0, 3000);
                sheet.SetColumnWidth(1, 10000);
                sheet.SetColumnWidth(2, 6000);
                sheet.SetColumnWidth(3, 6000);

                int iRow = 1;
                list.ForEach(r =>
                {
                    row = sheet.CreateRow(iRow);
                    row.CreateCell(0).SetCellValue(r.Business_Accounting_NO);
                    row.CreateCell(1).SetCellValue(r.Company_Name);
                    row.CreateCell(2).SetCellValue(r.Company_Status_Desc);
                    row.CreateCell(3).SetCellValue(r.Revoke_App_Date);
                    iRow++;
                });
                return new ResponseModel() { ReturnObject = workbook };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }


        private async Task<List<BusinessRegistryNewModel>> GetList(List<string> businessIds)
        {
            var businessRegistries = new List<BusinessRegistryNewModel>();
            foreach (var item in businessIds)
            {
                 var model = new BusinessRegistryNewModel();
                 model.Business_Accounting_NO = item;

                //公司登記
                var companyResult = await _companyRegistration.GetCompanyRegistration(item);
                if (string.IsNullOrEmpty(companyResult.Company_Name) == false)
                {
                    model.Company_Name = companyResult.Company_Name;
                    model.Company_Status_Desc = companyResult.Company_Status_Desc;
                    model.Revoke_App_Date = companyResult.Revoke_App_Date;
                    businessRegistries.Add(model);
                    continue;
                }

                //商業登記
                var businessResult = await _companyRegistration.GetBusinessRegistryModel(item);
                if(string.IsNullOrEmpty(businessResult.Business_Name) == false)
                {
                    model.Company_Name = businessResult.Business_Name;
                    model.Company_Status_Desc = businessResult.Business_Current_Status_Desc;
                }

                businessRegistries.Add(model);
            }

            return businessRegistries;
        }
    }
}
