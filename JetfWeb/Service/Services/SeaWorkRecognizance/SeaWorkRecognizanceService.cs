using Dapper;
using iTextSharp.text.io;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Extensions;
using Service.Models;
using Service.Models.SeaUnreceivedOrder;
using Service.Services.SeaUnreceivedOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaWorkRecognizance
{
    public class SeaWorkRecognizanceService : _BaseService
    {
        private readonly SeaUnreceivedOrderService _seaUnreceivedOrderService;

        public SeaWorkRecognizanceService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, SeaUnreceivedOrderService seaUnreceivedOrderService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _seaUnreceivedOrderService = seaUnreceivedOrderService;
        }

        /// <summary>
        /// 海快作業具結
        /// </summary>
        /// <param name="mainNumberList"></param>
        /// <returns></returns>
        public IWorkbook GetExcel(List<string> mainNumberList)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var details = GetCesMainOrderDetail(mainNumberList);

            details = details
                    .OrderBy(r => r.MAINNUMBER)
                    .ThenBy(r => r.BL_NO)
                    .ThenBy(r => r.ITEM_NO_SORT)
                    .ToList();

            var despatchNames = details
                .GroupBy(r => new { r.MAINNUMBER, r.DESPATCH_NAME })
                .Select(r =>
                new
                {
                    r.Key.MAINNUMBER,
                    r.Key.DESPATCH_NAME,
                });

            //總表
            _seaUnreceivedOrderService.GetDespatchNameReportSheet(workbook, details);

            foreach (var item in despatchNames)
            {
                var despatchNameList = details
                    .Where(r => r.MAINNUMBER == item.MAINNUMBER && r.DESPATCH_NAME == item.DESPATCH_NAME)
                    .OrderBy(r => r.MAINNUMBER)
                    .ThenBy(r => r.BL_NO)
                    .ThenBy(r => r.ITEM_NO_SORT)
                    .ToList();
                
                //明細
                _seaUnreceivedOrderService.GetDespatchNameDetailSheet(workbook, despatchNameList, true);
            }

            return workbook;
        }

        /// <summary>
        /// 取得海快具結
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="upload_ope"></param>
        /// <returns></returns>
        private List<CesMainOrderDetailModel> GetCesMainOrderDetail(List<string> mainNumberList)
        {
            if (mainNumberList.Count == 0)
                return new List<CesMainOrderDetailModel>();

            string sql = $@"
                            SELECT 
                            a.BagNumber as BL_NO,a.MAINNUMBER,a.CorrectImporterName,a.CorrectImporterId,a.CorrectImporterPhone,a.UploadOpe,
                            b.MANIFEST,
                            c.ITEM_NO,c.JETF_ID,c.TERMSOFPRICE,c.CURRENCY,c.GW,c.PIECE,c.PIECE_UNIT,c.MARKS,c.ITEM_NAME,c.CCC_CODE,c.TRADEMARK,c.II_SPEC,c.NW,c.QUANTITY,
                            c.QUANTITY_UNIT,c.UNIT_PRICE,c.INVOICE_AMOUNT,c.MEASUREMENT,c.CBM,c.MADEIN,c.EXPORTER,c.EX_COUNRTYCODE,c.EX_ADD,c.PARTY_IDENTIFIER,
                            c.IMPORTER_ID,c.IMPORTER,c.IM_PHONENO,
                            c.IM_ADD,c.CONT_TYPE as E_CONT_TYPE,c.CONT_NO as E_CONT_NO,c.SEALNO as E_SEALNO,c.DECLARATION_2,c.TAXFEE_DECLARED,c.TRANS_NAME,c.JETF_SERIAL,c.SIZE,
                            d.CUST_NAME as DESPATCH_NAME,
                            e.CONT_TYPE as O_CONT_TYPE,e.CONT_NO as O_CONT_NO,e.SEALNO as O_SEALNO,
	                        i.CONSOL_CODE,i.CONSOL_TYPE,i.CONSOL_NAME,i.CONSOL_URL
                            FROM [jetf].[dbo].CptSeaMainNumberDetail a
                            left join [jetf].[dbo].[SEA_MANIFEST_UPLOAD] b on a.MAINNUMBER=b.MAINNUMBER and a.BagNumber=b.BL_NO
                            left join [DATA_CENTER].[dbo].[SEA_ORDER_EDIT] c on a.MAINNUMBER=c.MAINNUMBER and a.BagNumber = c.BL_NO
                            left join [DATA_CENTER].[dbo].[SYS_CUST] d on c.DESPATCH_NAME = d.CUST_CODE
                            left join (select * from [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL] where GW > 0 ) e on a.MAINNUMBER=e.MAINNUMBER and a.BagNumber = e.BL_NO
                            left join [DATA_CENTER].[dbo].[CES_MAIN_ORDER] f on a.MAINNUMBER=f.MAIN_NUMBER and f.TYPE='ER'
                            left join [DATA_CENTER].[dbo].[SYS_PARAM] g on f.CLEARANCE_CP=g.CODE and g.TYPE='CLEARANCE_CP'
                            left join [jetf].[dbo].[SeaWorkErrorOrder] h on a.MainNumber = h.MainNumber and a.BagNumber = h.BagNumber
                            left join [DATA_CENTER].[dbo].[Sys_cust] i on c.DESPATCH_NAME= i.CUST_CODE
                            where a.MainNumber in ({string.Join(",", mainNumberList.Select(r => $"'{r}'"))})
                            and (g.NAME like '%TPCT%' or g.NAME ='台北貨櫃' or g.NAME ='基隆港務')
                            and d.CUST_NAME like N'%菜鳥%'
                            and
                            (
	                            a.Gb353RejReasonCode in ('B6D','B6E') or 
	                            exists (select * from [jetf].[dbo].[SeaWorkErrorOrder] h
			                            where a.MainNumber = h.MainNumber and a.BagNumber = h.BagNumber
			                            and h.Reason in('B6D','B6E'))
                            )
                        ";

            var result = conn.Query<CesMainOrderDetailModel>(sql, commandTimeout: 600).ToList();

            result.ForEach(r =>
            {
                r.IMPORTER = string.IsNullOrEmpty(r.UploadOpe) ? r.IMPORTER : r.CorrectImporterName;
                r.IMPORTER_ID = string.IsNullOrEmpty(r.UploadOpe) ? r.IMPORTER_ID : r.CorrectImporterId;
                r.IM_PHONENO = string.IsNullOrEmpty(r.UploadOpe) ? r.IM_PHONENO : r.CorrectImporterPhone;
            });

            return result;
        }

    }
}
