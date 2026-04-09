using Dapper;
using NPOI.SS.UserModel;
using Org.BouncyCastle.Asn1.Ocsp;
using Service.EnumTax;
using Service.Models;
using Service.Models.CptSeaMainNumberJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.CptStatusReport
{
    public class CptStatusReportService : _BaseService
    {
        private readonly CptTradeVanService _cptTradeVanService;

        public CptStatusReportService(CptTradeVanService cptTradeVanService)
        {
            _cptTradeVanService = cptTradeVanService;
        }

        public ResponseModel GetExecl(DataTypeEnum type, CptStatusEnum cptStatus, string startDate, string endDate)
        {
            try
            {
                if (DataTypeEnum.Sea == type)
                {
                    var list = GetCptSeaMainNumberDetails(cptStatus, startDate, endDate);
                    var workbook = _cptTradeVanService.GetCptSeaMainNumberDetailExcel(list);
                    return new ResponseModel() {ReturnObject = workbook };
                }
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
           
            return new ResponseModel("查無資料");
        }


        private List<CptSeaMainNumberDetailModel> GetCptSeaMainNumberDetails(CptStatusEnum cptStatus, string startDate, string endDate) 
        {
            var sql = @"
                        select * from [jetf].[dbo].CptSeaMainNumberDetail a
                        where  exists
                        (
	                        select MainNumber from [jetf].[dbo].[CptSeaMainNumber]
	                        where a.MainNumber = MainNumber and UploadTime between @StartDate and @EndDate 
	                        group by MainNumber
                        )
                        ";

            if (cptStatus == CptStatusEnum.UnreceivedOrder)
            {
                sql += " and IsReceiveOrder = '0'";
            }

            return conn.Query<CptSeaMainNumberDetailModel>(sql,
                new 
                { 
                    StartDate = $"{startDate} 00:00:00", 
                    EndDate = $"{endDate} 23:59:59.999",
                })
                .OrderBy(x => x.MainNumber)
                .ThenBy(x => x.BagNumber)
                .ToList();
        }
    }
}
