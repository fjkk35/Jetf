using Dapper;
using Service.Data;
using Service.Models;
using Service.Models.Shenzhen;
using Service.Services.SearchCargo.Domain;
using Service.EnumTax;
using Service.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace Service.Services.SearchCargo
{
    public class SearchCargoService : _BaseService
    {
        private const string AirSource = "Air";
        private const string SeaSource = "Sea";
        private readonly GlobalService _globalService;

        public SearchCargoService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext, GlobalService globalService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _globalService = globalService;
        }

        /// <summary>
        /// 查詢貨況列表
        /// </summary>
        public List<SearchCargoResponse> SearchCargo(SearchCargoRequest request)
        {
            string searchValue = request.SearchValue?.Trim() ?? "";

            if (string.IsNullOrEmpty(searchValue))
            {
                return new List<SearchCargoResponse>();
            }

            List<CargoQueryRowModel> list = new List<CargoQueryRowModel>();

            switch (request.SearchType)
            {
                case "phone":
                    list = GetCargoRowsByPhone(searchValue);
                    break;
                case "invoice":
                    list = GetCargoRowsByDeliveryNo(searchValue);
                    if (list.Count == 0)
                    {
                        var trackingNo = GetShenzhenCargoTrackingNo(searchValue);
                        if (!string.IsNullOrEmpty(trackingNo))
                        {
                            list = GetCargoRowsByBagNo(trackingNo);
                        }
                    }
                    break;
                case "trackingNo":
                    list = GetCargoRowsByJetfSerial(searchValue);
                    if (list.Count == 0)
                    {
                        list = GetCargoRowsByBagNo(searchValue);
                    }
                    break;
                case "fieldX":
                    string bagNo = GetOriginallist_BagNo(searchValue);
                    if (!string.IsNullOrEmpty(bagNo))
                    {
                        list = GetCargoRowsByBagNo(bagNo);
                    }
                    break;
                case "cainiaoFieldX":
                    var deliveryNoList = GetOriginallist_DeliverynoListByFieldX(searchValue);
                    if (deliveryNoList.Count > 0)
                    {
                        list = GetCargoRowsByDeliveryNoList(deliveryNoList);
                    }
                    break;
                case "orderNo":
                    string deliveryno = GetOriginallist_Deliveryno(searchValue);
                    if (!string.IsNullOrEmpty(deliveryno))
                    {
                        list = GetCargoRowsByDeliveryNo(deliveryno);
                    }
                    break;
            }

            // 格式化資料
            foreach (var item in list)
            {
                item.Format_OUT_DATETIME = item.I_SIGN_OUT_TIME?.ToString("yyyy-MM-dd") ?? "";
                item.F_INCLUDE_TAX = _globalService.GetTaxType(item.F_INCLUDE_TAX ?? "");
            }

            var result = list.Select(row => new SearchCargoResponse
            {
                Id = row.Id,
                Source = row.Source,
                F_DataDate = row.F_DataDate,
                I_DATA_TYPE = row.I_DATA_TYPE,
                Format_OUT_DATETIME = row.Format_OUT_DATETIME,
                CUSTOMER = row.CUSTOMER,
                MAINNUMBER = row.MAINNUMBER,
                BL_NO = row.BL_NO,
                PIECE = row.PIECE,
                DELIVERYNO = row.DELIVERYNO,
                ITEM_NAME = row.ITEM_NAME
            }).ToList();

            return result;
        }

        /// <summary>
        /// 取得貨況明細
        /// </summary>
        public CargoDetailResponse GetCargoDetail(string source, string id)
        {
            var data = GetCargoRowBySourceAndId(source, id);
            if (data == null)
            {
                return null;
            }

            var detail = new CargoDetailResponse
            {
                Id = data.Id,
                ETA = data.ETA,
                GW = data.GW,
                PIECE = data.PIECE,
                Main_Number = data.MAINNUMBER,
                Bag_Number = data.BL_NO,
                Cust_Id = data.DESPATCH_NAME,
                Cust_Name = data.CUSTOMER,
                Trans_Name = data.TRANS_NAME,
                Trans_Name_New = data.TRANS_NAME_NEW,
                Dlv_Inv = data.JETF_SERIAL,
                Deliveryno = data.DELIVERYNO,
                Recipient = data.IMPORTER,
                Recphone = data.IM_PHONENO,
                Recaddress = data.IM_ADD,
                CC = data.CC,
                Field_X = data.FIELD_X,
                Order_No = data.ORDER_NO,
                Express_No = data.EXPRESS_NO,
                TrackingNo = data.TRACKINGNO,
                Status = GetStatusDescription(data.ORIGINAL, data.STATUS, data.TRACKINGNO),
            };

            //取得製單資料
            //空運
            if (data.ORIGINAL?.ToUpper() == "ETL")
            {
                detail.ETA = DataCenterDb.MainOrderInfos
                    .AsNoTracking()
                    .Where(x => x.MainNumber == detail.Main_Number && x.DeliveryDate.HasValue)
                    .Select(x => x.DeliveryDate)
                    .FirstOrDefault();

                var airMakeList = GetAirMakeListData(detail.Main_Number, detail.TrackingNo);
                if (airMakeList != null && airMakeList.Count > 0)
                {
                    // 申報：取第一筆
                    detail.ActualDeclarant = airMakeList[0].RECIPIENT?.Trim();
                    // 申報人電話：取第一筆
                    detail.ActualDeclarantPhone = airMakeList[0].RECPHONE?.Trim();
                    // 申報金額：加總所有 UNITPRICE
                    detail.ActualInvoiceAmount = airMakeList
                        .Where(x => x.UNITPRICE.HasValue)
                        .Sum(x => x.UNITPRICE.Value);
                    // 申報品名：取所有品名
                    detail.ActualItemNameList = airMakeList
                        .Where(x => !string.IsNullOrEmpty(x.ITEMS))
                        .Select(x => x.ITEMS.Trim())
                        .ToList();
                }
            }

            //海運
            if (data.ORIGINAL?.ToUpper() == "SEA")
            { 
                var seaOrderEdit = GetSeaOrderEditData(detail.Main_Number, detail.Bag_Number);
                if (seaOrderEdit != null && seaOrderEdit.Count > 0)
                {
                    // 申報：取 GW > 0 的第一筆
                    var firstRecord = seaOrderEdit.FirstOrDefault(x => x.GW.HasValue && x.GW.Value > 0);
                    if (firstRecord != null)
                    {
                        detail.ActualDeclarant = firstRecord.IMPORTER;
                        detail.ActualDeclarantPhone = firstRecord.IM_PHONENO;
                    }

                    // 申報金額：加總所有 Invoice_Amount
                    detail.ActualInvoiceAmount = seaOrderEdit
                        .Where(x => x.Invoice_Amount.HasValue)
                        .Sum(x => x.Invoice_Amount.Value);
                    // 申報品名：取所有品名
                    detail.ActualItemNameList = seaOrderEdit
                        .Where(x => !string.IsNullOrEmpty(x.ITEM_NAME))
                        .Select(x => x.ITEM_NAME.Trim())
                        .ToList();
                }

                // 優先使用 CptSeaMainNumberDetail 申報人資料
                var cptSeaDetail = GetCptSeaMainNumberDetail(detail.Main_Number, detail.Bag_Number);
                if (cptSeaDetail != null)
                {
                    // 使用修正後的申報人資料
                    if (!string.IsNullOrEmpty(cptSeaDetail.CorrectImporterName))
                    {
                        detail.ActualDeclarant = cptSeaDetail.CorrectImporterName;
                    }
                    if (!string.IsNullOrEmpty(cptSeaDetail.CorrectImporterPhone))
                    {
                        detail.ActualDeclarantPhone = cptSeaDetail.CorrectImporterPhone;
                    }
                }
            }

            // 取得進出倉時間
            var clearanceInfo = GetClearanceInfo(detail.Main_Number, detail.Bag_Number)
                ?? GetClearanceInfoByJetfSerial(detail.Main_Number, detail.Dlv_Inv);
            if (clearanceInfo != null)
            {
                detail.In_Datetime = clearanceInfo.SIGN_IN_TIME;
                detail.Out_Datetime = clearanceInfo.SIGN_OUT_TIME;
                detail.Source = clearanceInfo.DATA_TYPE;
                detail.Type = clearanceInfo.CLEARANCE_TYPE;
            }

            // 依分提單號及物流貨號取得同一筆稅金資料。
            var feeMaster = GetFeeMaster(detail.TrackingNo, detail.Deliveryno);

            if (feeMaster != null)
            {
                detail.Include_Tax = feeMaster.IncludeTax;
                detail.Tax1 = feeMaster.Tax1.ToString();
                detail.Tax2 = feeMaster.Tax2.ToString();
                detail.TotalTax = feeMaster.TotalTax.ToString();
                detail.CCFee = feeMaster.CcFee.ToString();
                detail.Fee = feeMaster.Fee.ToString();
                detail.Cod = feeMaster.Cod.ToString();
                detail.To_Dlv_Cod = feeMaster.ToDlvCod.ToString();
                detail.CustomerCod = feeMaster.CustomerCod.ToString();
                detail.TransCod = feeMaster.TransCod.ToString();
            }

            // 取得回款進度
            var trackingNo = detail.TrackingNo?.Trim();
            var deliveryNo = detail.Deliveryno?.Trim();
            if (!string.IsNullOrWhiteSpace(trackingNo)
                && !string.IsNullOrWhiteSpace(deliveryNo))
            {
                var reconciliationInvoice = JetfDb.ReconciliationInvoices
                    .AsNoTracking()
                    .Where(x => x.TrackingNo == trackingNo
                        && x.DlvInv == deliveryNo)
                    .OrderByDescending(x => x.UpdatedTime ?? x.CreatedTime)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefault();

                if (reconciliationInvoice != null)
                {
                    detail.ReconciliationInvoiceProgress = new ReconciliationInvoiceProgressModel
                    {
                        PaymentDate = null,
                        Type = reconciliationInvoice.Type,
                        Date = reconciliationInvoice.Date,
                        Invoice = reconciliationInvoice.Invoice
                    };
                }
            }
            // 取得稅單編號
            var taxNumbers = GetTaxNumber(data.ORIGINAL, detail.Bag_Number, detail.Dlv_Inv);
            detail.TaxNumberList = taxNumbers.Select(x => x.TAX_NUMBER).ToList();

            // 取得掃貨上車資料
            var scanCargo = GetPdtScanCargoUpload(detail.Bag_Number, detail.Dlv_Inv);
            if (scanCargo != null)
            {
                detail.ScanCargoUploadTime = scanCargo.UploadTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                detail.ScanCargoUploadOpe = scanCargo.UploadOpe?.Trim();
                detail.ScanCargoTransName = scanCargo.TransName?.Trim();
                detail.ScanCargoCarNo = scanCargo.CarNo?.Trim();
            }

            // 取得錯單類別
            var errorReasons = GetErrorReason(data.ORIGINAL, detail.Main_Number, detail.Bag_Number, detail.Dlv_Inv);
            detail.ErrorReason = string.Join("，", errorReasons.Select(x => x.REASON).Distinct());

            // 配送進度
            var cargoStatusList = GetCargo_Status_Detail(detail.Deliveryno);
            detail.CargoStatusList = cargoStatusList.Select(x => new CargoStatusItem
            {
                Time = x.TRANS_MODIFY_TIME?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                Status = x.TRANS_STATUS_DESC
            }).ToList();

            // 記錄LOG
            InsertLog_Cargo_Status(new LogCargoStatusModel
            {
                Dlv_Inv = detail.Dlv_Inv,
                Remark = "貨況查詢",
                Search_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                User_Ip = GetIPAddress(),
                User_Id = GetUserId()
            });

            return detail;
        }

        /// <summary>
        /// 取得處置說明資料
        /// </summary>
        public List<ProcessInfoModel> GetProcess(string dlv_Inv)
        {
            string sql = @"
                select a.ID, a.REMARK, a.FILENAME, a.FILEPATH, a.CRTDATETIME, a.FINISH,
                       b.[USER_NAME] as [USER_NAME],
                       c.[USER_NAME] as FINISH_USER_NAME,
                       a.PROCESS_TYPE as PROCESS_TYPE_RAW
                from jetf.dbo.Process a
                left join jetf.dbo.[USER_MASTER] b on a.[USER_ID]=b.[USER_ID]
                left join jetf.dbo.[USER_MASTER] c on a.FINISH_USER_ID=c.[USER_ID]
                where DLV_INV=@DLV_INV and DEL='0' 
                order by CRTDATETIME desc";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                var result = connection.Query<ProcessInfoModel>(sql, new { DLV_INV = dlv_Inv }).ToList();

                foreach (var item in result)
                {
                    item.FormatCrtDateTime = item.CRTDATETIME?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    item.PROCESS_TYPE = string.IsNullOrEmpty(item.PROCESS_TYPE_RAW)
                        ? "貨況"
                        : item.PROCESS_TYPE_RAW.ToEnum<CargoProcessType>().ToDescription();
                }

                return result;
            }
        }

        /// <summary>
        /// 新增處置說明
        /// </summary>
        public ResponseModel InsertProcess(ProcessModel model)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "新增成功";

            string sql = @"
                insert [jetf].[dbo].[Process]([MID],[DATADATE],[DLV_INV],[REMARK],[FILEPATH],[FILENAME],[USER_ID],[PROCESS_TYPE]) 
                values(@MID,@DATADATE,@DLV_INV,@REMARK,@FILEPATH,@FILENAME,@USER_ID,@PROCESS_TYPE)";

            try
            {
                using (var connection = new SqlConnection(conn.ConnectionString))
                {
                    connection.Execute(sql, new
                    {
                        DATADATE = model.DataDate,
                        MID = model.MId,
                        DLV_INV = model.Dlv_Inv,
                        REMARK = model.Remark,
                        FILEPATH = model.FilePath ?? "",
                        FILENAME = model.FileName ?? "",
                        USER_ID = model.User_Id,
                        PROCESS_TYPE = model.Process_Type ?? "1"
                    });
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return resopnseModel;
        }

        /// <summary>
        /// 處置說明結案
        /// </summary>
        public ResponseModel FinishProcess(string dlv_inv, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "結案成功";

            string sql = @"
                update [jetf].[dbo].[Process] 
                set FINISH='Y',FINISH_USER_ID=@FINISH_USER_ID,FINISH_DATETIME=getdate() 
                where DLV_INV=@DLV_INV and FINISH='N'";

            try
            {
                using (var connection = new SqlConnection(conn.ConnectionString))
                {
                    connection.Execute(sql, new
                    {
                        DLV_INV = dlv_inv,
                        FINISH_USER_ID = user_Id
                    });
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return resopnseModel;
        }

        /// <summary>
        /// 刪除處置說明
        /// </summary>
        public ResponseModel DeleteProcess(string id, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "刪除成功";

            string sql = @"
                update [jetf].[dbo].[Process] 
                set DEL='1',DEL_USER_ID=@DEL_USER_ID,DELDATETIME=getdate() 
                where ID=@ID";

            try
            {
                using (var connection = new SqlConnection(conn.ConnectionString))
                {
                    connection.Execute(sql, new
                    {
                        ID = id,
                        DEL_USER_ID = user_Id
                    });
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return resopnseModel;
        }

        /// <summary>
        /// 取得貨況查詢紀錄
        /// </summary>
        public List<LogCargoStatusModel> GetLogCargoStatus(string dlv_inv)
        {
            string sql = @"
                select DLV_INV, SEARCH_TIME, USER_ID, USER_IP, REMARK
                from [jetf].[dbo].[LOG_CARGO_STATUS] 
                where DLV_INV=@DLV_INV and REMARK='貨況查詢'
                order by SEARCH_TIME desc";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                return connection.Query<LogCargoStatusModel>(sql, new { DLV_INV = dlv_inv }).ToList();
            }
        }

        /// <summary>
        /// 取得通關袋號明細
        /// </summary>
        public List<CargoTargetBagNumberModel> GetCargoTargetBagNumber(string bagNumber)
        {
            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                var result = connection.Query<CargoTargetBagNumberModel>(
                    "[jetf].[dbo].[USP_GetCargoTargetBagNumber]",
                    new { BagNumber = bagNumber },
                    commandType: System.Data.CommandType.StoredProcedure
                ).ToList();

                // 格式化時間
                foreach (var item in result)
                {
                    item.Format_SIGN_IN_TIME = item.SIGN_IN_TIME?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    item.Format_SIGN_OUT_TIME = item.SIGN_OUT_TIME?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                }

                return result;
            }
        }

        /// <summary>
        /// 取得通關分提單號明細
        /// </summary>
        public List<CargoTargetTrackingNoModel> GetCargoTargetTrackingNo(string bagNumber)
        {
            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                var result = connection.Query<CargoTargetTrackingNoModel>(
                    "[jetf].[dbo].[USP_GetCargoTargetTrackingNo]",
                    new { BagNumber = bagNumber },
                    commandType: System.Data.CommandType.StoredProcedure
                ).ToList();

                // 格式化時間
                foreach (var item in result)
                {
                    item.Format_SIGN_IN_TIME = item.SIGN_IN_TIME?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    item.Format_SIGN_OUT_TIME = item.SIGN_OUT_TIME?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                }

                return result;
            }
        }

        /// <summary>
        /// 取得速派新遞貨號資料
        /// </summary>
        public List<ShenzhenCargoModel> GetShenzhenCargoByTrackingNo(string trackingNo)
        {
            string sql = @"
                SELECT TrackingNo, DeliveryNo 
                FROM [jetf].[dbo].ShenzhenCargo 
                WHERE TrackingNo = @TrackingNo";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                return connection.Query<ShenzhenCargoModel>(sql, new { TrackingNo = trackingNo }).ToList();
            }
        }

        #region Private Methods

        /// <summary>
        /// 取得貨況-電話
        /// </summary>
        private List<CargoQueryRowModel> GetCargoRowsByPhone(string phone)
        {
            var phoneCandidates = BuildPhoneCandidates(phone);
            var airRows = DataCenterDb.OriginalLists
                .AsNoTracking()
                .Where(x => phoneCandidates.Contains(x.RecPhone))
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            var seaRows = DataCenterDb.SeaOrderOriginals
                .AsNoTracking()
                .Where(x => phoneCandidates.Contains(x.ImporterPhone))
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            return BuildCargoQueryRows(airRows, seaRows);
        }

        /// <summary>
        /// 取得貨況-Id
        /// </summary>
        private CargoQueryRowModel GetCargoRowBySourceAndId(string source, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            source = NormalizeSource(source);

            if (source == AirSource)
            {
                if (!int.TryParse(id, out var airId))
                {
                    return null;
                }

                var airRow = DataCenterDb.OriginalLists
                    .AsNoTracking()
                    .FirstOrDefault(x => x.Id == airId);

                return airRow == null
                    ? null
                    : BuildCargoQueryRows(new[] { airRow }, Enumerable.Empty<SeaOrderOriginalEntity>()).FirstOrDefault();
            }

            if (!int.TryParse(id, out var seaId))
            {
                return null;
            }

            var seaRow = DataCenterDb.SeaOrderOriginals
                .AsNoTracking()
                .FirstOrDefault(x => x.RowId == seaId);

            return seaRow == null
                ? null
                : BuildCargoQueryRows(Enumerable.Empty<OriginalListEntity>(), new[] { seaRow }).FirstOrDefault();
        }

        /// <summary>
        /// 取得貨況-物流貨號
        /// </summary>
        private List<CargoQueryRowModel> GetCargoRowsByDeliveryNo(string deliveryno)
        {
            var airRows = DataCenterDb.OriginalLists
                .AsNoTracking()
                .Where(x => x.DeliveryNo == deliveryno)
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            var seaRows = DataCenterDb.SeaOrderOriginals
                .AsNoTracking()
                .Where(x => x.JetfSerial == deliveryno)
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            return BuildCargoQueryRows(airRows, seaRows);
        }

        /// <summary>
        /// 取得貨況-多筆物流貨號
        /// </summary>
        private List<CargoQueryRowModel> GetCargoRowsByDeliveryNoList(IEnumerable<string> deliveryNoList)
        {
            var deliveryNos = (deliveryNoList ?? Enumerable.Empty<string>())
                .Where(deliveryNo => !string.IsNullOrWhiteSpace(deliveryNo))
                .Select(deliveryNo => deliveryNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!deliveryNos.Any())
            {
                return new List<CargoQueryRowModel>();
            }

            var airRows = DataCenterDb.OriginalLists
                .AsNoTracking()
                .Where(x => deliveryNos.Contains(x.DeliveryNo))
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            var seaRows = DataCenterDb.SeaOrderOriginals
                .AsNoTracking()
                .Where(x => deliveryNos.Contains(x.JetfSerial))
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            return BuildCargoQueryRows(airRows, seaRows);
        }

        /// <summary>
        /// 取得貨況-分提單號
        /// </summary>
        private List<CargoQueryRowModel> GetCargoRowsByJetfSerial(string jetf_Serial)
        {
            var airRows = DataCenterDb.OriginalLists
                .AsNoTracking()
                .Where(x => x.TrackingNo == jetf_Serial)
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            var seaRows = DataCenterDb.SeaOrderOriginals
                .AsNoTracking()
                .Where(x => x.JetfSerial == jetf_Serial)
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            return BuildCargoQueryRows(airRows, seaRows);
        }

        /// <summary>
        /// 取得貨況-袋號
        /// </summary>
        private List<CargoQueryRowModel> GetCargoRowsByBagNo(string bl_No)
        {
            var airRows = DataCenterDb.OriginalLists
                .AsNoTracking()
                .Where(x => x.BagNo == bl_No)
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            var seaRows = DataCenterDb.SeaOrderOriginals
                .AsNoTracking()
                .Where(x => x.BlNo == bl_No)
                .OrderByDescending(x => x.CreateDate)
                .ToList();

            return BuildCargoQueryRows(airRows, seaRows);
        }

        /// <summary>
        /// 取得使用速派物流貨號取得上傳分提單號
        /// </summary>
        private string GetShenzhenCargoTrackingNo(string deliveryNo)
        {
            string sql = "select TrackingNo from ShenzhenCargo where DeliveryNo = @DeliveryNo";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                return connection.QueryFirstOrDefault<string>(sql, new { DeliveryNo = deliveryNo }) ?? "";
            }
        }

        /// <summary>
        /// 使用客戶外箱號，回傳袋號
        /// </summary>
        private string GetOriginallist_BagNo(string field_X)
        {
            return DataCenterDb.OriginalLists
                .AsNoTracking()
                .Where(x => x.FieldX == field_X)
                .Select(x => x.BagNo)
                .FirstOrDefault()?.Trim() ?? "";
        }

        /// <summary>
        /// 使用客戶外箱號(菜鳥)，回傳多筆物流貨號
        /// </summary>
        private List<string> GetOriginallist_DeliverynoListByFieldX(string field_X)
        {
            return DataCenterDb.OriginalLists
                .AsNoTracking()
                .Where(x => x.FieldX == field_X)
                .Select(x => x.DeliveryNo)
                .ToList()
                .Where(deliveryNo => !string.IsNullOrWhiteSpace(deliveryNo))
                .Select(deliveryNo => deliveryNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 使用客戶訂單號，回傳物流貨號
        /// </summary>
        private string GetOriginallist_Deliveryno(string Order_No)
        {
            return DataCenterDb.OriginalLists
                .AsNoTracking()
                .Where(x => x.OrderNo == Order_No)
                .Select(x => x.DeliveryNo)
                .FirstOrDefault()?.Trim() ?? "";
        }

        /// <summary>
        /// 依分提單號及物流貨號取得稅金資料。
        /// </summary>
        /// <param name="trackingNo">分提單號。</param>
        /// <param name="deliveryNo">物流貨號。</param>
        /// <returns>稅金資料；找不到相符的複合鍵時回傳 <see langword="null"/>。</returns>
        private FeeMasterModel GetFeeMaster(string trackingNo, string deliveryNo)
        {
            if (string.IsNullOrWhiteSpace(trackingNo)
                || string.IsNullOrWhiteSpace(deliveryNo))
            {
                return null;
            }

            const string sql = @"
                select *
                from [jetf].[dbo].[FEE_MASTER]
                where TRACKINGNO = @TRACKINGNO
                  and DLV_INV = @DLV_INV";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                var row = connection.QueryFirstOrDefault(sql, new
                {
                    TRACKINGNO = trackingNo.Trim(),
                    DLV_INV = deliveryNo.Trim()
                });

                if (row == null)
                    return null;

                int tax1, tax2, ccFee, fee, cod, toDlvCod, customerCod, transCod;

                var model = new FeeMasterModel()
                {
                    DataDate = row.DATADATE?.ToString(),
                    IncludeTax = row.INCLUDE_TAX?.ToString(),
                    Tax1 = Int32.TryParse(row.TAX1?.ToString(), out tax1) ? tax1 : 0,
                    Tax2 = Int32.TryParse(row.TAX2?.ToString(), out tax2) ? tax2 : 0,
                    CcFee = Int32.TryParse(row.CCFEE?.ToString(), out ccFee) ? ccFee : 0,
                    Fee = Int32.TryParse(row.FEE?.ToString(), out fee) ? fee : 0,
                    Cod = Int32.TryParse(row.COD?.ToString(), out cod) ? cod : 0,
                    ToDlvCod = Int32.TryParse(row.TO_DLV_COD?.ToString(), out toDlvCod) ? toDlvCod : 0,
                    CustomerCod = Int32.TryParse(row.CUSTOMER_COD?.ToString(), out customerCod) ? customerCod : 0,
                    TransCod = Int32.TryParse(row.TRANS_COD?.ToString(), out transCod) ? transCod : 0,
                };

                model.TotalTax = model.Tax1 + model.Tax2;
                return model;
            }
        }

        /// <summary>
        /// 取得物流配送狀態
        /// </summary>
        private List<CargoStatusDetailModel> GetCargo_Status_Detail(string trans_number)
        {
            if (string.IsNullOrEmpty(trans_number))
                return new List<CargoStatusDetailModel>();

            string sql = "select * from [DATA_CENTER].[dbo].[CARGO_STATUS_DETAIL] (nolock) where TRANS_NUMBER=@TRANS_NUMBER order by TRANS_MODIFY_TIME desc";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                return connection.Query<CargoStatusDetailModel>(sql, new { TRANS_NUMBER = trans_number }).ToList();
            }
        }

        /// <summary>
        /// 新增貨況查詢記錄
        /// </summary>
        private void InsertLog_Cargo_Status(LogCargoStatusModel model)
        {
            string sql = @"
         insert [jetf].[dbo].[LOG_CARGO_STATUS]([DLV_INV],[SEARCH_TIME],[REMARK],[USER_ID],[USER_IP]) 
         values(@DLV_INV,@SEARCH_TIME,@REMARK,@USER_ID,@USER_IP)";

            try
            {
                using (var connection = new SqlConnection(conn.ConnectionString))
                {
                    connection.Execute(sql, new
                    {
                        DLV_INV = model.Dlv_Inv,
                        SEARCH_TIME = model.Search_Time,
                        REMARK = model.Remark,
                        USER_ID = model.User_Id,
                        USER_IP = model.User_Ip
                    });
                }
            }
            catch (Exception)
            {
                // 記錄LOG失敗不影響主流程
            }
        }

        /// <summary>
        /// 查詢稅金編號
        /// </summary>
        private List<TaxNumberModel> GetTaxNumber(string original, string bagNumber, string dlv_Inv)
        {
            string sql;
            object parameters;

            // 空運用dlv_Inv查詢
            if (original?.ToUpper() == "ETL")
            {
                sql = "SELECT distinct TAX_NUMBER FROM [DATA_CENTER].[dbo].[CLEARANCE_TAX] where MERGE_NUMBER=@MERGE_NUMBER";
                parameters = new { MERGE_NUMBER = dlv_Inv };
            }
            else
            {
                // 海運用bagNumber
                sql = "SELECT distinct TAX_NUMBER FROM [DATA_CENTER].[dbo].[CLEARANCE_TAX] where MERGE_NUMBER=@MERGE_NUMBER";
                parameters = new { MERGE_NUMBER = bagNumber };
            }

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                var result = connection.Query<TaxNumberModel>(sql, parameters).ToList();

                // 空運用袋號查稅金編號
                if (result.Count == 0 && original?.ToUpper() == "ETL")
                {
                    sql = "SELECT distinct TAX_NUMBER FROM [DATA_CENTER].[dbo].[CLEARANCE_TAX] where MERGE_NUMBER=@BAG_NUMBER";
                    result = connection.Query<TaxNumberModel>(sql, new { BAG_NUMBER = bagNumber }).ToList();
                }

                return result;
            }
        }

        /// <summary>
        /// 查詢掃貨上車時間、人員
        /// </summary>
        private PdtScanCargoUploadModel GetPdtScanCargoUpload(string bagNumber, string dlv_Inv)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select top 1 b.TransName,a.CarNo,a.UploadTime,a.UploadOpe ");
            sb.Append("from [jetf].[dbo].[PdtScanCargoUpload] a ");
            sb.Append("join [jetf].[dbo].[PdtTrans] b on a.TransNo=b.TransNo ");
            sb.Append("where a.Data=@bagNumber ");

            if (!string.IsNullOrEmpty(dlv_Inv))
            {
                sb.Append("or Data=@dlv_Inv ");
            }

            sb.Append("order by a.UploadTime desc");


            return conn.QueryFirstOrDefault<PdtScanCargoUploadModel>(sb.ToString(), new
            {
                bagNumber,
                dlv_Inv
            });

        }

        /// <summary>
        /// 取得海運製單資料
        /// </summary>
        private List<SeaOrderEditModel> GetSeaOrderEditData(string mainNumber, string blNo)
        {
            string sql = @"
                SELECT IMPORTER, IM_PHONENO, ITEM_NAME, Invoice_Amount, GW 
                FROM DATA_CENTER.[dbo].[SEA_ORDER_EDIT]
                WHERE MAINNUMBER = @MAINNUMBER AND BL_NO = @BL_NO";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                return connection.Query<SeaOrderEditModel>(sql, new
                {
                    MAINNUMBER = mainNumber,
                    BL_NO = blNo
                }).ToList();
            }
        }

        /// <summary>
        /// 取得空運製單資料
        /// </summary>
        private List<AirMakeListModel> GetAirMakeListData(string mainNumber, string trackingNo)
        {
            string sql = @"
                SELECT RECIPIENT, RECPHONE, ITEMS, UNITPRICE 
                FROM DATA_CENTER.dbo.MAKELIST
                WHERE MAINNUMBER = @MAINNUMBER AND TRACKINGNO = @TRACKINGNO";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                return connection.Query<AirMakeListModel>(sql, new
                {
                    MAINNUMBER = mainNumber,
                    TRACKINGNO = trackingNo
                }).ToList();
            }
        }

        /// <summary>
        /// 取得進出倉時間
        /// </summary>
        private ClearanceInfoModel GetClearanceInfo(string mainNumber, string mergeNumber)
        {
            var sql = @"
                select top 1 DATA_TYPE,CLEARANCE_TYPE,SIGN_IN_TIME,SIGN_OUT_TIME from [DATA_CENTER].[dbo].[CLEARANCE_INFO] (nolock)
                where MAIN_NUMBER=@MainNumber and MERGE_NUMBER=@MergeNumber
                order by SIGN_IN_TIME desc";

            return conn.QueryFirstOrDefault<ClearanceInfoModel>(sql, new
            {
                MainNumber = mainNumber,
                MergeNumber = mergeNumber
            });
        }

        /// <summary>
        /// 取得進出倉時間(物流貨號)
        /// </summary>
        /// <param name="mainNumber"></param>
        /// <param name="jetfSerial"></param>
        /// <returns></returns>
        private ClearanceInfoModel GetClearanceInfoByJetfSerial(string mainNumber, string jetfSerial)
        {
            var sql = @"
                select top 1 DATA_TYPE,CLEARANCE_TYPE,SIGN_IN_TIME,SIGN_OUT_TIME from [DATA_CENTER].[dbo].[CLEARANCE_INFO] (nolock)
                where MAIN_NUMBER=@MainNumber and MERGE_NUMBER=@JetfSerial
                order by SIGN_IN_TIME desc";

            return conn.QueryFirstOrDefault<ClearanceInfoModel>(sql, new
            {
                MainNumber = mainNumber,
                JetfSerial = jetfSerial
            });
        }


        /// <summary>
        /// 取得錯單類別
        /// </summary>
        private List<ErrorReasonModel> GetErrorReason(string original, string mainNumber, string bagNumber, string dlv_Inv)
        {
            string sql;

            if (original?.ToUpper() == "ETL")
            {
                sql = @"
              select REASON from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] 
         where MAWB=@MainNumber and HAWB=@Dlv_Inv 
union 
        select REASON from [DATA_CENTER].[dbo].[ETL_PLINK_ERROR] 
            where MAWB=@MainNumber and BAG_NO=@BagNumber and HAWB=''";
            }
            else
            {
                sql = @"
        select MESSAGE as REASON from [jetf].[dbo].[SEA_BAGNO_UPLOAD] 
           where MAINNUMBER=@MainNumber and BL_NO=@BagNumber 
order by CRTDATETIME desc";
            }

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                return connection.Query<ErrorReasonModel>(sql, new
                {
                    MainNumber = mainNumber,
                    BagNumber = bagNumber,
                    Dlv_Inv = dlv_Inv
                }).ToList();
            }
        }

        /// <summary>
        /// 取得狀態描述
        /// </summary>
        private string GetStatusDescription(string original, string status, string trackingNo)
        {
            // 空運
            if (original?.ToUpper() == "ETL")
            {
                var model = GetAirDetainModel(trackingNo);
                if (!string.IsNullOrEmpty(model))
                {
                    if (model == "DU")
                    {
                        return "出口地扣留";
                    }
                    else if (model == "GF")
                    {
                        return "G類無ID";
                    }
                    return model;
                }
                return status;
            }

            // 海運
            if (original?.ToUpper() == "SEA")
            {
                if (status == "D")
                {
                    return "出口地扣留";
                }
                return status;
            }

            return status;
        }

        /// <summary>
        /// 取得空運扣留模式
        /// </summary>
        private string GetAirDetainModel(string trackingNo)
        {
            if (string.IsNullOrEmpty(trackingNo))
            {
                return "";
            }

            string sql = "SELECT MODEL FROM [DATA_CENTER].[dbo].[AIR_DETAIN] WHERE TRACKINGNO=@TRACKINGNO";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                return connection.QueryFirstOrDefault<string>(sql, new { TRACKINGNO = trackingNo }) ?? "";
            }
        }

        private List<string> BuildPhoneCandidates(string phone)
        {
            string phone2 = phone.Substring(1);
            string phone3 = "886" + phone;
            string phone4 = "886" + phone.Substring(1);
            string phone5 = "+886" + phone;
            string phone6 = "+886" + phone.Substring(1);
            string phone11 = "00886" + phone;
            string phone12 = "00886" + phone.Substring(1);
            string phone7;
            string phone8;
            string phone9;
            string phone10;

            if (phone.Length > 7)
            {
                phone7 = "886-" + phone.Insert(6, "-").Insert(2, "-");
                phone8 = "886-" + phone.Substring(1).Insert(5, "-").Insert(1, "-");
                phone9 = "+886-" + phone.Insert(6, "-").Insert(2, "-");
                phone10 = "+886-" + phone.Substring(1).Insert(5, "-").Insert(1, "-");
            }
            else
            {
                phone7 = phone;
                phone8 = phone;
                phone9 = phone;
                phone10 = phone;
            }

            return new[]
            {
                phone,
                phone2,
                phone3,
                phone4,
                phone5,
                phone6,
                phone7,
                phone8,
                phone9,
                phone10,
                phone11,
                phone12
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        }

        private List<CargoQueryRowModel> BuildCargoQueryRows(
            IEnumerable<OriginalListEntity> airRows,
            IEnumerable<SeaOrderOriginalEntity> seaRows)
        {
            var airList = (airRows ?? Enumerable.Empty<OriginalListEntity>()).ToList();
            var seaList = (seaRows ?? Enumerable.Empty<SeaOrderOriginalEntity>()).ToList();

            var airCustomerLookup = BuildAirCustomerLookup(airList);
            var airCustomerNames = GetAirCustomerNames(airList.Select(x => x.DespatchNo));
            var seaCustomerLookup = BuildSeaCustomerLookup(seaList);
            var seaCustomerNames = GetSeaCustomerNames(seaList.Select(x => x.DespatchName));

            var list = airList.Select(row => MapAirCargoQueryRow(row, airCustomerLookup, airCustomerNames))
                .Concat(seaList.Select(row => MapSeaCargoQueryRow(row, seaCustomerLookup, seaCustomerNames)))
                .GroupBy(row => row.Source + ":" + row.Id)
                .Select(group => group.First())
                .ToList();

            ApplyClearanceInfo(list);
            ApplyFeeMasterInfo(list);

            return list
                .OrderByDescending(row => row.I_SIGN_OUT_TIME ?? row.SOURCE_CREATEDATE)
                .ThenByDescending(row => row.SOURCE_CREATEDATE)
                .ThenByDescending(row => row.Id)
                .ToList();
        }

        private CargoQueryRowModel MapAirCargoQueryRow(
            OriginalListEntity row,
            IReadOnlyDictionary<string, CustomerMasterEntity> customerLookup,
            IReadOnlyDictionary<string, string> customerNames)
        {
            var customerCode = PadCustomerCode(row.DespatchNo);
            var lookupKey = BuildCompositeKey(customerCode, row.TransTaxPayment);
            customerLookup.TryGetValue(lookupKey, out var customer);

            var customerName = customer?.Customer;
            if (string.IsNullOrWhiteSpace(customerName) && !string.IsNullOrWhiteSpace(row.DespatchNo))
            {
                customerNames.TryGetValue(row.DespatchNo.Trim(), out customerName);
            }

            var transName = customer?.TransName;
            if (string.IsNullOrWhiteSpace(transName))
            {
                transName = row.Dispatcher;
            }

            return new CargoQueryRowModel
            {
                Id = row.Id.ToString(),
                Source = AirSource,
                ORIGINAL = "ETL",
                ETA = null,
                GW = FormatNullableNumber(row.BagWeight) ?? FormatNullableNumber(row.Weight),
                PIECE = row.Pieces?.ToString() ?? string.Empty,
                F_DataDate = string.Empty,
                I_DATA_TYPE = string.Empty,
                I_CLEARANCE_TYPE = string.Empty,
                DESPATCH_NAME = customerCode,
                CUSTOMER = customerName ?? string.Empty,
                I_SIGN_IN_TIME = row.SignInTime,
                I_SIGN_OUT_TIME = null,
                MAINNUMBER = row.MainNumber,
                BL_NO = row.BagNo,
                JETF_SERIAL = row.TrackingNo,
                F_TAX_NUMBER = string.Empty,
                TRANS_NAME = transName ?? string.Empty,
                IMPORTER = row.Recipient,
                IM_PHONENO = row.RecPhone,
                IM_ADD = row.RecAddress,
                F_INCLUDE_TAX = row.IncludeTax,
                F_CCFEE = string.Empty,
                F_FEE = string.Empty,
                F_COD = string.Empty,
                F_TAX1 = string.Empty,
                F_TAX2 = string.Empty,
                F_TO_DLV_COD = string.Empty,
                ITEM_NAME = row.Items,
                CC = row.Cc,
                DELIVERYNO = row.DeliveryNo,
                FIELD_X = row.FieldX,
                TRANS_TAXPAYMENT = row.TransTaxPayment,
                TRANS_NAME_NEW = row.TransTaxPayment ?? string.Empty,
                ORDER_NO = row.OrderNo,
                EXPRESS_NO = row.ExpressNo,
                TRACKINGNO = row.TrackingNo,
                STATUS = row.Status,
                SOURCE_CREATEDATE = row.CreateDate
            };
        }

        private CargoQueryRowModel MapSeaCargoQueryRow(
            SeaOrderOriginalEntity row,
            IReadOnlyDictionary<string, CustomerMasterEntity> customerLookup,
            IReadOnlyDictionary<string, string> customerNames)
        {
            var lookupKey = BuildCompositeKey(row.DespatchName, row.TransName);
            customerLookup.TryGetValue(lookupKey, out var customer);

            var customerName = customer?.Customer;
            if (string.IsNullOrWhiteSpace(customerName) && !string.IsNullOrWhiteSpace(row.DespatchName))
            {
                customerNames.TryGetValue(row.DespatchName.Trim(), out customerName);
            }

            return new CargoQueryRowModel
            {
                Id = row.RowId.ToString(),
                Source = SeaSource,
                ORIGINAL = "SEA",
                ETA = row.Eta,
                GW = FormatNullableNumber(row.Gw),
                PIECE = row.Piece?.ToString() ?? string.Empty,
                F_DataDate = string.Empty,
                I_DATA_TYPE = string.Empty,
                I_CLEARANCE_TYPE = string.Empty,
                DESPATCH_NAME = row.DespatchName,
                CUSTOMER = customerName ?? string.Empty,
                I_SIGN_IN_TIME = null,
                I_SIGN_OUT_TIME = null,
                MAINNUMBER = row.MainNumber,
                BL_NO = row.BlNo,
                JETF_SERIAL = row.JetfSerial,
                F_TAX_NUMBER = string.Empty,
                TRANS_NAME = row.TransName,
                IMPORTER = row.Importer,
                IM_PHONENO = row.ImporterPhone,
                IM_ADD = row.ImporterAddress,
                F_INCLUDE_TAX = row.IncludeTax,
                F_CCFEE = string.Empty,
                F_FEE = string.Empty,
                F_COD = string.Empty,
                F_TAX1 = string.Empty,
                F_TAX2 = string.Empty,
                F_TO_DLV_COD = string.Empty,
                ITEM_NAME = row.ItemName,
                CC = row.Cc?.ToString(),
                DELIVERYNO = row.JetfSerial,
                FIELD_X = string.Empty,
                TRANS_TAXPAYMENT = row.TransTaxPayment,
                TRANS_NAME_NEW = row.TransTaxPayment,
                ORDER_NO = string.Empty,
                EXPRESS_NO = string.Empty,
                TRACKINGNO = row.BlNo,
                STATUS = row.Status,
                SOURCE_CREATEDATE = row.CreateDate
            };
        }

        private void ApplyClearanceInfo(IEnumerable<CargoQueryRowModel> list)
        {
            foreach (var item in list ?? Enumerable.Empty<CargoQueryRowModel>())
            {
                var clearanceInfo = GetClearanceInfo(item.MAINNUMBER, item.BL_NO)
                    ?? GetClearanceInfoByJetfSerial(item.MAINNUMBER, item.JETF_SERIAL);

                if (clearanceInfo == null)
                {
                    continue;
                }

                item.I_DATA_TYPE = clearanceInfo.DATA_TYPE ?? item.I_DATA_TYPE;
                item.I_CLEARANCE_TYPE = clearanceInfo.CLEARANCE_TYPE ?? item.I_CLEARANCE_TYPE;
                item.I_SIGN_IN_TIME = clearanceInfo.SIGN_IN_TIME ?? item.I_SIGN_IN_TIME;
                item.I_SIGN_OUT_TIME = clearanceInfo.SIGN_OUT_TIME ?? item.I_SIGN_OUT_TIME;
            }
        }

        /// <summary>
        /// 依分提單號及物流貨號批次比對 FEE_MASTER，回填貨況清單的稅金作業日。
        /// </summary>
        /// <param name="list">貨況清單資料。</param>
        private void ApplyFeeMasterInfo(IEnumerable<CargoQueryRowModel> list)
        {
            // TRACKINGNO 為分提單號，DELIVERYNO 為物流貨號；任一欄空白皆無法組成有效查詢條件。
            var cargoRows = (list ?? Enumerable.Empty<CargoQueryRowModel>())
                .Where(x => !string.IsNullOrWhiteSpace(x.TRACKINGNO)
                    && !string.IsNullOrWhiteSpace(x.DELIVERYNO))
                .ToList();

            if (!cargoRows.Any())
            {
                return;
            }

            // 使用複合鍵批次查詢，確保 TRACKINGNO 與 DLV_INV 必須同時符合 FEE_MASTER 的同一筆資料。
            var feeMasterRows = JetfDb.FeeMasters
                .AsNoTracking()
                .WhereBulkContains(
                    JetfDb,
                    cargoRows,
                    x => new { x.TrackingNo, x.DlvInv },
                    x => new
                    {
                        TrackingNo = x.TRACKINGNO,
                        DlvInv = x.DELIVERYNO
                    });

            var feeMasterLookup = feeMasterRows.ToLookup(x => new
            {
                TrackingNo = (x.TrackingNo ?? string.Empty).Trim().ToUpperInvariant(),
                DlvInv = (x.DlvInv ?? string.Empty).Trim().ToUpperInvariant()
            });

            // 依相同複合鍵找回稅金資料，避免相同物流貨號但不同分提單號時回填錯誤資料。
            foreach (var item in cargoRows)
            {
                var key = new
                {
                    TrackingNo = item.TRACKINGNO.Trim().ToUpperInvariant(),
                    DlvInv = item.DELIVERYNO.Trim().ToUpperInvariant()
                };
                var feeMaster = feeMasterLookup[key].FirstOrDefault();

                if (feeMaster == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(feeMaster.DataDate))
                {
                    item.F_DataDate = feeMaster.DataDate;
                }
            }
        }

        private Dictionary<string, CustomerMasterEntity> BuildAirCustomerLookup(IEnumerable<OriginalListEntity> rows)
        {
            var customerCodes = rows
                .Select(x => PadCustomerCode(x.DespatchNo))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var transNos = rows
                .Select(x => x.TransTaxPayment)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!customerCodes.Any() || !transNos.Any())
            {
                return new Dictionary<string, CustomerMasterEntity>();
            }

            return JetfDb.CustomerMasters
                .AsNoTracking()
                .Where(x => x.TranType == "空運" && customerCodes.Contains(x.CustId) && transNos.Contains(x.TransNo))
                .ToList()
                .GroupBy(x => BuildCompositeKey(x.CustId, x.TransNo))
                .ToDictionary(group => group.Key, group => group.First());
        }

        private Dictionary<string, CustomerMasterEntity> BuildSeaCustomerLookup(IEnumerable<SeaOrderOriginalEntity> rows)
        {
            var customerCodes = rows
                .Select(x => x.DespatchName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var transNames = rows
                .Select(x => x.TransName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!customerCodes.Any() || !transNames.Any())
            {
                return new Dictionary<string, CustomerMasterEntity>();
            }

            return JetfDb.CustomerMasters
                .AsNoTracking()
                .Where(x => x.TranType == "海運" && customerCodes.Contains(x.CustId) && transNames.Contains(x.TransName))
                .ToList()
                .GroupBy(x => BuildCompositeKey(x.CustId, x.TransName))
                .ToDictionary(group => group.Key, group => group.First());
        }

        private static string PadCustomerCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().PadLeft(5, '0');
        }

        private static string BuildCompositeKey(string left, string right)
        {
            return (left ?? string.Empty).Trim() + "|" + (right ?? string.Empty).Trim();
        }

        private static string FormatNullableNumber(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.##") : string.Empty;
        }

        private static string NormalizeSource(string source)
        {
            if (string.Equals(source, SeaSource, StringComparison.OrdinalIgnoreCase))
            {
                return SeaSource;
            }

            return AirSource;
        }

        /// <summary>
        /// 取得CPT海運主提單明細資料（修正後的申報人資料）
        /// </summary>
        private CptSeaMainNumberDetailModel GetCptSeaMainNumberDetail(string mainNumber, string bagNumber)
        {
            string sql = @"
                SELECT CorrectImporterName, CorrectImporterPhone
                FROM [jetf].[dbo].[CptSeaMainNumberDetail]
                WHERE MainNumber = @MainNumber AND BagNumber = @BagNumber";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                return connection.QueryFirstOrDefault<CptSeaMainNumberDetailModel>(sql, new
                {
                    MainNumber = mainNumber,
                    BagNumber = bagNumber
                });
            }
        }

        #endregion
    }

    /// <summary>
    /// 稅金資料Model
    /// </summary>
    internal class FeeMasterModel
    {
        public string DataDate { get; set; }
        public string IncludeTax { get; set; }
        public int Tax1 { get; set; }
        public int Tax2 { get; set; }
        public int TotalTax { get; set; }
        public int CcFee { get; set; }
        public int Fee { get; set; }
        public int Cod { get; set; }
        public int ToDlvCod { get; set; }
        public int CustomerCod { get; set; }
        public int TransCod { get; set; }
    }

    /// <summary>
    /// 進出倉資訊Model (對應 CLEARANCE_INFO)
    /// </summary>
    internal class ClearanceInfoModel
    {
        public string DATA_TYPE { get; set; }
        public string CLEARANCE_TYPE { get; set; }
        public DateTime? SIGN_IN_TIME { get; set; }
        public DateTime? SIGN_OUT_TIME { get; set; }
    }

    /// <summary>
    /// 海運製單資料Model (對應 SEA_ORDER_EDIT)
    /// </summary>
    internal class SeaOrderEditModel
    {
        public string IMPORTER { get; set; }
        public string IM_PHONENO { get; set; }
        public string ITEM_NAME { get; set; }
        public decimal? Invoice_Amount { get; set; }
        public decimal? GW { get; set; }
    }

    /// <summary>
    /// 空運製單資料Model (對應 MAKELIST)
    /// </summary>
    internal class AirMakeListModel
    {
        public string RECIPIENT { get; set; }
        public string RECPHONE { get; set; }
        public string ITEMS { get; set; }
        public decimal? UNITPRICE { get; set; }
    }

    /// <summary>
    /// CPT海運主提單明細Model (對應 CptSeaMainNumberDetail)
    /// </summary>
    internal class CptSeaMainNumberDetailModel
    {
        public string CorrectImporterName { get; set; }
        public string CorrectImporterPhone { get; set; }
    }

}
