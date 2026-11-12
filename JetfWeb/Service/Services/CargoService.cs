using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.Data;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CargoService
    {
        private readonly SqlConnection conn;
        private readonly GlobalService _globalService;
        private readonly JetfDbContext _jetfDb;
        private readonly DataCenterDbContext _dataCenterDb;
        /// <summary>
        /// 建構式
        /// </summary>
        public CargoService(GlobalService globalService, JetfDbContext jetfDb, DataCenterDbContext dataCenterDb)
        {
            _globalService = globalService;
            _jetfDb = jetfDb;
            _dataCenterDb = dataCenterDb;
            conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
        }

        /// <summary>
        /// 取得貨況
        /// </summary>
        /// <returns></returns>
        public DataTable GetFee_Master(string invoice)
        {
            DateTime date;
            StringBuilder sb = new StringBuilder();
            sb.Append("select DATADATE,SOURCE,TYPE,isnull(b.CUST_NAME,a.CUSTOMER) as CUST_NAME,IN_DATETIME,OUT_DATETIME,MAIN_NUMBER,TRACKINGNO as 'BAG_NUMBER',DLV_INV,TAX_NUMBER,DLV_COM as 'TRANS_NAME',RECIPIENT,RECPHONE,RECADDRESS,a.INCLUDE_TAX,CCFEE,a.FEE,a.COD,a.TAX1,a.TAX2,TO_DLV_COD from [jetf].[dbo].[FEE_MASTER] a ");
            sb.Append("left join Data_center.dbo.sys_cust b on a.CUSTOMER=b.CUST_CODE ");
            sb.Append("where [SOURCE_TYPE] in('1','2') and (DLV_INV=@DLV_INV or TRACKINGNO=@DLV_INV) ");
            sb.Append("union all ");
            sb.Append("select DATADATE,SOURCE,TYPE,b.CUSTOMER,IN_DATETIME,OUT_DATETIME,MAIN_NUMBER,BAG_NUMBER,DLV_INV,TAX_NUMBER,b.TRANS_NAME,RECIPIENT,RECPHONE,RECADDRESS,a.INCLUDE_TAX,CCFEE,a.FEE,a.COD,a.TAX1,a.TAX2,TO_DLV_COD from [jetf].[dbo].[FEE_MASTER] a ");
            sb.Append("left join jetf.dbo.customer_master b on [jetf].[dbo].[PadLeft]('0',a.customer,5)=b.CUST_ID and a.dlv_com=b.TRANS_NO and b.TRAN_TYPE='空運' ");
            sb.Append("where[SOURCE_TYPE]='3' and (DLV_INV=@DLV_INV or BAG_NUMBER=@DLV_INV) ");

            DataTable dt = new DataTable();
            if (invoice != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = invoice;
                    da.Fill(dt);
                }

                //if (dt.Rows.Count == 0)
                //{
                //    dt = GetSource_Fee_Master(invoice);
                //}
            }

            dt.Columns.Add("Format_OUT_DATETIME", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (DateTime.TryParse(dt.Rows[i]["OUT_DATETIME"].ToString(), out date))
                {
                    dt.Rows[i]["Format_OUT_DATETIME"] = date.ToString("yyyy-MM-dd");
                }
                dt.Rows[i]["INCLUDE_TAX"] = _globalService.GetTaxType(dt.Rows[i]["INCLUDE_TAX"].ToString());
            }

            return dt;
        }

        /// <summary>
        /// 取得物流配送狀態
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns></returns>
        public DataTable GetCargo_Status_Detail(string trans_number)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select * from [DATA_CENTER].[dbo].[CARGO_STATUS_DETAIL] (nolock) where TRANS_NUMBER=@TRANS_NUMBER order by TRANS_MODIFY_TIME desc  ");
            DataTable dt = new DataTable();
            if (trans_number != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@TRANS_NUMBER", SqlDbType.NVarChar).Value = trans_number;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 新增貨況查詢記錄
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseModel InsertLog_Cargo_Status(LogCargoStatusModel model)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;

            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[LOG_CARGO_STATUS]([DLV_INV],[SEARCH_TIME],[REMARK],[USER_ID],[USER_IP]) ");
            sb.Append("values(@DLV_INV,@SEARCH_TIME,@REMARK,@USER_ID,@USER_IP) ");
            using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
            {
                try
                {
                    conn.Open();
                    cmd.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = model.Dlv_Inv;
                    cmd.Parameters.Add("@SEARCH_TIME", SqlDbType.NVarChar).Value = model.Search_Time;
                    cmd.Parameters.Add("@REMARK", SqlDbType.NVarChar).Value = model.Remark;
                    cmd.Parameters.Add("@USER_ID", SqlDbType.NVarChar).Value = model.User_Id;
                    cmd.Parameters.Add("@USER_IP", SqlDbType.NVarChar).Value = model.User_Ip;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = ex.Message;
                }
                finally
                {
                    conn.Close();
                }
            }
            return resopnseModel;
        }

        /// <summary>
        /// 取得貨況查詢記錄
        /// </summary>
        /// <param name="dlv_inv"></param>
        /// <returns></returns>
        public DataTable GetLog_Cargo_Status(string dlv_inv)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select  * from  [jetf].[dbo].[LOG_CARGO_STATUS] where DLV_INV=@DLV_INV and REMARK='貨況查詢' ");
            DataTable dt = new DataTable();
            if (dlv_inv != "")
            {
                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
                {
                    da.SelectCommand.Parameters.Add("@DLV_INV", SqlDbType.NVarChar).Value = dlv_inv;
                    da.Fill(dt);
                }
            }
            return dt;
        }

        /// <summary>
        /// 新增批量貨況查詢明細表
        /// </summary>
        /// <param name="dt_Upload"></param>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public ResponseModel InsertBatchSearchCargo2(DataTable dt_Upload, string upload_time, string user_Id)
        {
            ResponseModel resopnseModel = new ResponseModel();
            resopnseModel.status = Status.success;
            resopnseModel.msg = "新增成功";

            DateTime date = DateTime.Now;
            string dataDate = date.ToString("yyyyMMdd");
            StringBuilder sb = new StringBuilder();
            sb.Append("insert [jetf].[dbo].[BatchSearchCargo2](TrackingNo, Upload_Time, Upload_Ope) ");
            sb.Append("values(@TrackingNo, @Upload_Time, @Upload_Ope) ");

            using (SqlTransaction tran = conn.BeginTransaction())
            {
                using (SqlCommand cmd = new SqlCommand(sb.ToString(), conn))
                {
                    cmd.Transaction = tran;
                    try
                    {
                        for (int i = 0; i < dt_Upload.Rows.Count; i++)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add("@TrackingNo", SqlDbType.NVarChar).Value = dt_Upload.Rows[i]["TrackingNo"].ToString();
                            cmd.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                            cmd.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                            cmd.ExecuteNonQuery();
                        }
                        //確認寫入
                        tran.Commit();
                        resopnseModel.status = Status.success;
                    }
                    catch (Exception ex)
                    {
                        resopnseModel.status = Status.error;
                        resopnseModel.msg = ex.Message;
                        //取消寫入
                        tran.Rollback();
                    }
                }
            }

            return resopnseModel;
        }

        /// <summary>
        /// 取得批量貨況查詢明細表
        /// </summary>
        /// <param name="upload_time"></param>
        /// <param name="user_Id"></param>
        /// <returns></returns>
        public DataTableModel GetBatchSearchCargo2(string upload_time, string user_Id)
        {
            DataTable dt = new DataTable();
            DataTableModel dataTableModel = new DataTableModel();
            dataTableModel.status = Status.success;
            dataTableModel.msg = "成功";
            try
            {
                using (SqlDataAdapter da = new SqlDataAdapter("[jetf].[dbo].[USP_GetBatchSearchCargo]", conn))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add("@Upload_Ope", SqlDbType.NVarChar).Value = user_Id;
                    da.SelectCommand.Parameters.Add("@Upload_Time", SqlDbType.NVarChar).Value = upload_time;
                    da.Fill(dt);
                }
                dataTableModel.dt = dt;
            }
            catch (Exception ex)
            {
                dataTableModel.status = Status.error;
                dataTableModel.msg = ex.Message;
            }

            return dataTableModel;
        }


        /// <summary>
        /// 依日期、客戶、處置類型與結案狀態查詢資料，組合處置說明報表。
        /// </summary>
        /// <param name="custId">客戶代碼；傳入 All 時不限制客戶。</param>
        /// <param name="sdate">查詢起日，格式為 yyyyMMdd。</param>
        /// <param name="edate">查詢迄日，格式為 yyyyMMdd。</param>
        /// <param name="processType">處置類型；傳入 All 時不限制類型。</param>
        /// <param name="finish">結案狀態；傳入 All 時不限制狀態。</param>
        /// <returns>包含查詢狀態、訊息與報表資料列的結果。</returns>
        public ProcessReportResultModel ProcessReport(string custId, string sdate, string edate, string processType, string finish)
        {
            var result = new ProcessReportResultModel();
            var jetfTimeout = _jetfDb.Database.CommandTimeout;
            var dataCenterTimeout = _dataCenterDb.Database.CommandTimeout;
            try
            {
                _jetfDb.Database.CommandTimeout = 600;
                _dataCenterDb.Database.CommandTimeout = 600;

                // 取得處置紀錄，保留原本的日期與建立時間排序。
                var report = _jetfDb.Processes.AsNoTracking()
                    .Where(row => row.Deleted == "0" && row.DataDate.CompareTo(sdate) >= 0
                        && row.DataDate.CompareTo(edate) <= 0)
                    .Where(row => processType == "All" || (processType != null && row.ProcessType == processType))
                    .Where(row => finish == "All" || (finish != null && row.Finish == finish))
                    .OrderBy(row => row.DataDate)
                    .ThenBy(row => row.CreateDateTime)
                    .Select(row => new ProcessReportRowModel
                    {
                        DataDate = row.DataDate,
                        DlvInv = row.DlvInv,
                        Remark = row.Remark,
                        UserId = row.UserId,
                        CreateDateTime = row.CreateDateTime,
                        Deleted = row.Deleted,
                        Finish = row.Finish,
                        FinishUserId = row.FinishUserId,
                        FinishDateTime = row.FinishDateTime,
                        ProcessType = row.ProcessType
                    })
                    .ToList();
                if (report.Count == 0) return result;

                var cargoDic = GetProcessCargoDic(report.Select(row => row.DlvInv), custId);
                if (custId != "All")
                    report.RemoveAll(row => !cargoDic.ContainsKey(row.DlvInv?.Trim() ?? ""));

                // 客戶名稱與人員姓名各查詢一次，組合時只讀取字典。
                var customerDic = GetProcessCustomerDic();
                var userIds = report.SelectMany(row => new[] { row.UserId, row.FinishUserId })
                    .Where(userId => !string.IsNullOrWhiteSpace(userId))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                var userDic = _jetfDb.UserMasters.AsNoTracking()
                    .WhereBulkContains(_jetfDb, userIds, row => row.UserId, key => key,
                        row => new { row.UserId, row.UserName })
                    .GroupBy(row => row.UserId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First().UserName,
                        StringComparer.OrdinalIgnoreCase);

                foreach (var row in report)
                {
                    userDic.TryGetValue(row.UserId ?? "", out var userName);
                    userDic.TryGetValue(row.FinishUserId ?? "", out var finishUserName);
                    row.UserName = userName;
                    row.FinishUserName = finishUserName;

                    // All 保留找不到來源貨況的處置紀錄，其貨況欄位維持空白。
                    if (!cargoDic.TryGetValue(row.DlvInv?.Trim() ?? "", out var cargo)) continue;

                    var customerKey = Tuple.Create(cargo.TransportType, (cargo.CustomerCode ?? "").Trim().ToUpperInvariant());
                    customerDic.TryGetValue(customerKey, out var customer);
                    row.CusdId = cargo.CustomerCode;
                    row.Customer = string.IsNullOrEmpty(customer) ? cargo.CustomerCode?.Trim() ?? "" : customer;
                    row.MainNumber = cargo.MainNumber;
                    row.BlNo = cargo.BagNo;
                    row.Importer = cargo.Importer;
                    row.ImPhoneNo = cargo.ImPhoneNo;
                    row.Eta = cargo.Eta;
                    row.IDataType = cargo.DataType;
                    row.ISignInTime = cargo.SignInTime;
                    row.ISignOutTime = cargo.SignOutTime;
                }

                result.Rows = report;
            }
            catch (Exception ex)
            {
                result.status = Status.error;
                result.msg = ex.Message;
            }
            finally
            {
                _jetfDb.Database.CommandTimeout = jetfTimeout;
                _dataCenterDb.Database.CommandTimeout = dataCenterTimeout;
            }
            return result;
        }

        /// <summary>
        /// 查詢海空運原始貨況、到港日與清關資訊，建立每個貨號的最新資料字典。
        /// </summary>
        /// <param name="dlvInvs">處置紀錄中的物流貨號。</param>
        /// <param name="custId">客戶代碼；傳入 All 時不限制客戶。</param>
        /// <returns>以物流貨號為鍵的完整貨況資料。</returns>
        private Dictionary<string, ProcessReportCargoModel> GetProcessCargoDic(IEnumerable<string> dlvInvs, string custId)
        {
            var numbers = dlvInvs.Select(number => number?.Trim())
                .Where(number => !string.IsNullOrEmpty(number))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 兩種來源直接投影為相同 Model；空運貨號統一使用 TRACKINGNO。
            var sea = _dataCenterDb.SeaOrderOriginals.AsNoTracking()
                .Where(row => row.JetfSerial != null)
                .WhereBulkContains(_dataCenterDb, numbers, row => row.JetfSerial, key => key,
                    row => new ProcessReportCargoModel
                    {
                        Id = row.RowId,
                        CreateDate = row.CreateDate,
                        TransportType = "SEA",
                        DlvInv = row.JetfSerial,
                        MainNumber = row.MainNumber,
                        BagNo = row.BlNo,
                        CustomerCode = row.DespatchName,
                        Eta = row.Eta,
                        Importer = row.Importer,
                        ImPhoneNo = row.ImporterPhone
                    });
            var air = _dataCenterDb.OriginalLists.AsNoTracking()
                .Where(row => row.TrackingNo != null)
                .WhereBulkContains(_dataCenterDb, numbers, row => row.TrackingNo, key => key,
                    row => new ProcessReportCargoModel
                    {
                        Id = row.Id,
                        CreateDate = row.CreateDate,
                        TransportType = "AIR",
                        DlvInv = row.TrackingNo,
                        MainNumber = row.MainNumber,
                        BagNo = row.BagNo,
                        CustomerCode = row.DespatchNo,
                        Importer = row.Recipient,
                        ImPhoneNo = row.RecPhone
                    });

            // 同貨號先取最新來源，再篩選客戶；空運同時接受原碼與補足五碼的代碼。
            var customerId = custId?.Trim();
            var cargos = sea.Concat(air)
                .GroupBy(cargo => cargo.DlvInv.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(cargo => cargo.CreateDate)
                    .ThenByDescending(cargo => cargo.Id)
                    .ThenBy(cargo => cargo.TransportType, StringComparer.Ordinal)
                    .First())
                .Where(cargo => custId == "All"
                    || string.Equals(cargo.CustomerCode?.Trim(), customerId, StringComparison.OrdinalIgnoreCase)
                    || (cargo.CustomerCode == null && customerId == "")
                    || (cargo.TransportType == "AIR" && !string.IsNullOrWhiteSpace(cargo.CustomerCode)
                        && string.Equals(cargo.CustomerCode.Trim().PadLeft(5, '0'), customerId, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var mainOrderDic = _dataCenterDb.MainOrderInfos.AsNoTracking()
                .Where(row => row.MainNumber != null && row.DeliveryDate != null)
                .WhereBulkContains(_dataCenterDb,
                    cargos.Where(cargo => cargo.TransportType == "AIR").Select(cargo => cargo.MainNumber).Distinct(),
                    row => row.MainNumber, key => key,
                    row => new { row.MainNumber, row.DeliveryDate, row.CreateTime, row.Id })
                .GroupBy(row => row.MainNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(row => row.CreateTime)
                    .ThenByDescending(row => row.Id).First().DeliveryDate, StringComparer.OrdinalIgnoreCase);

            var clearanceDic = GetProcessClearanceDic(cargos);

            foreach (var cargo in cargos)
            {
                if (cargo.TransportType == "AIR" && mainOrderDic.TryGetValue(cargo.MainNumber?.Trim() ?? "", out var eta))
                    cargo.Eta = eta;

                if (!clearanceDic.TryGetValue(cargo.DlvInv.Trim(), out var clearance)) continue;
                cargo.DataType = clearance.DataType;
                cargo.SignInTime = clearance.SignInTime;
                cargo.SignOutTime = clearance.SignOutTime;
            }

            return cargos.ToDictionary(cargo => cargo.DlvInv.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 一次讀取 SYS_CUST 客戶資料，依海運代碼及空運舊代碼建立名稱字典。
        /// </summary>
        /// <returns>以運別及客戶代碼組成鍵值的客戶名稱字典。</returns>
        private Dictionary<Tuple<string, string>, string> GetProcessCustomerDic()
        {
            var customers = _dataCenterDb.SysCusts.AsNoTracking()
                .Select(row => new { row.CustType, row.CustCode, row.OldCode, row.CustName })
                .ToList();

            var seaCustomerNames = customers
                .Where(row => row.CustType == "SEA" && !string.IsNullOrWhiteSpace(row.CustCode))
                .Select(row => new { Key = Tuple.Create("SEA", row.CustCode.Trim().ToUpperInvariant()), row.CustName });
            var airCustomerNames = customers
                .Where(row => row.CustType == "AIR" && !string.IsNullOrWhiteSpace(row.OldCode))
                .Select(row => new { Key = Tuple.Create("AIR", row.OldCode.Trim().ToUpperInvariant()), row.CustName });

            return seaCustomerNames.Concat(airCustomerNames)
                .GroupBy(row => row.Key)
                .ToDictionary(group => group.Key,
                    group => group.Select(row => row.CustName).FirstOrDefault() ?? string.Empty);
        }

        /// <summary>
        /// 取得各貨號對應的清關資料，海運比對主號及袋號，空運依貨號、FTZ 袋號及一般袋號依序比對。
        /// </summary>
        /// <param name="cargos">已選定來源與客戶的貨況資料。</param>
        /// <returns>以物流貨號為鍵的清關資料字典。</returns>
        private Dictionary<string, ClearanceInfoEntity> GetProcessClearanceDic(IEnumerable<ProcessReportCargoModel> cargos)
        {
            var keys = cargos.Select(cargo => new
            {
                cargo.TransportType,
                DlvInv = cargo.DlvInv.Trim().ToUpperInvariant(),
                MainNumber = (cargo.MainNumber ?? "").Trim().ToUpperInvariant(),
                BagNo = (cargo.BagNo ?? "").Trim().ToUpperInvariant()
            }).Where(key => key.MainNumber.Length > 0).ToList();
            var clearanceDic = new Dictionary<string, ClearanceInfoEntity>(StringComparer.OrdinalIgnoreCase);
            if (keys.Count == 0) return clearanceDic;

            var seaKeys = keys.Where(key => key.TransportType == "SEA" && key.BagNo.Length > 0)
                .Select(key => new { key.MainNumber, BagNumber = key.BagNo }).Distinct();
            var airKeys = keys.Where(key => key.TransportType == "AIR").ToList();
            var airBagKeys = airKeys.Where(key => key.BagNo.Length > 0)
                .Select(key => new { key.MainNumber, BagNumber = key.BagNo }).Distinct().ToList();

            // 四種查詢共用相同欄位投影，只讀取報表與最新資料判斷需要的欄位。
            Expression<Func<ClearanceInfoEntity, ClearanceInfoEntity>> projection = row => new ClearanceInfoEntity
            {
                RowId = row.RowId,
                MainNumber = row.MainNumber,
                BagNumber = row.BagNumber,
                MergeNumber = row.MergeNumber,
                DataType = row.DataType,
                SignInTime = row.SignInTime,
                SignOutTime = row.SignOutTime,
                ModifyTime = row.ModifyTime,
                ModifySeq = row.ModifySeq
            };
            var query = _dataCenterDb.ClearanceInfos.AsNoTracking()
                .Where(row => row.MainNumber != null);
            var airQuery = query.Where(row => row.ModifyFile != null && row.ModifyFile != "MANUAL");

            // 海運：主號 + 袋號。
            var seaRows = query.Where(row => row.BagNumber != null)
                .WhereBulkContains(_dataCenterDb, seaKeys,
                    row => new { row.MainNumber, row.BagNumber },
                    key => new { key.MainNumber, key.BagNumber }, projection);

            // 空運：主號 + 貨號，對應清關資料的 MERGE_NUMBER。
            var airRows = airQuery.Where(row => row.MergeNumber != null)
                .WhereBulkContains(_dataCenterDb,
                    airKeys.Where(key => key.DlvInv.Length > 0)
                        .Select(key => new { key.MainNumber, MergeNumber = key.DlvInv }).Distinct(),
                    row => new { row.MainNumber, row.MergeNumber },
                    key => new { key.MainNumber, key.MergeNumber }, projection);

            // 空運 FTZ：主號 + 袋號，對應清關資料的 MERGE_NUMBER。
            var ftzRows = airQuery.Where(row => row.DataType == "FTZ" && row.MergeNumber != null)
                .WhereBulkContains(_dataCenterDb,
                    airBagKeys.Select(key => new { key.MainNumber, MergeNumber = key.BagNumber }),
                    row => new { row.MainNumber, row.MergeNumber },
                    key => new { key.MainNumber, key.MergeNumber }, projection);

            // 空運袋號備援：MERGE_NUMBER 空白時，以主號 + BAG_NUMBER 比對。
            var bagRows = airQuery.Where(row => row.MergeNumber == "" && row.BagNumber != null)
                .WhereBulkContains(_dataCenterDb, airBagKeys,
                    row => new { row.MainNumber, row.BagNumber },
                    key => new { key.MainNumber, key.BagNumber }, projection);

            // 各比對規則分開取最新一筆，避免不同規則的資料互相覆蓋。
            var matches = seaRows.Select(row => (Rule: "SEA", Number: row.BagNumber, Info: row))
                .Concat(airRows.Select(row => (Rule: "AIR", Number: row.MergeNumber, Info: row)))
                .Concat(ftzRows.Select(row => (Rule: "FTZ", Number: row.MergeNumber, Info: row)))
                .Concat(bagRows.Select(row => (Rule: "BAG", Number: row.BagNumber, Info: row)));
            var latestDic = matches
                .GroupBy(match => (match.Rule,
                    MainNumber: match.Info.MainNumber.Trim().ToUpperInvariant(),
                    Number: match.Number.Trim().ToUpperInvariant()))
                .ToDictionary(group => group.Key, group => group
                    .OrderByDescending(match => match.Info.ModifyTime)
                    .ThenByDescending(match => match.Info.ModifySeq)
                    .ThenByDescending(match => match.Info.RowId)
                    .First().Info);

            foreach (var key in keys)
            {
                ClearanceInfoEntity clearance;
                if (key.TransportType == "SEA")
                    latestDic.TryGetValue(("SEA", key.MainNumber, key.BagNo), out clearance);
                else if (!latestDic.TryGetValue(("AIR", key.MainNumber, key.DlvInv), out clearance)
                    && !latestDic.TryGetValue(("FTZ", key.MainNumber, key.BagNo), out clearance))
                    latestDic.TryGetValue(("BAG", key.MainNumber, key.BagNo), out clearance);

                if (clearance != null)
                    clearanceDic[key.DlvInv] = clearance;
            }
            return clearanceDic;
        }

        /// <summary>
        /// 批量貨況查詢明細表上傳
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ResponseModel BatchSearchCargo2(string filePath, string fileName, string userId)
        {
            ResponseModel resopnseModel = new ResponseModel();

            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                //讀取檔案
                DataTable dt_Upload = ReadExcelBatchSearchCargo2(filePath);

                //新增
                if (dt_Upload.Rows.Count > 0)
                {
                    //寫入資料
                    string upload_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    resopnseModel = InsertBatchSearchCargo2(dt_Upload, upload_time, userId);

                    if (resopnseModel.status == Status.success)
                    {
                        //resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
                        resopnseModel.msg = $"{upload_time}︿{userId}";
                    }
                }
                else
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = $"上傳檔案筆數：{dt_Upload.Rows.Count}";
                }
            }
            finally
            {
                conn.Close();
            }
            return resopnseModel;
        }

        /// <summary>
        /// 讀取批量貨況查詢明細表上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        DataTable ReadExcelBatchSearchCargo2(string filePath)
        {
            DataRow dr;
            DataTable dt_Data = new DataTable();
            dt_Data.Columns.Add("TrackingNo", typeof(string));

            bool read = false;
            string trackingno;
            IWorkbook workBook;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                workBook = new XSSFWorkbook(fs);
            }

            var sheet = workBook.GetSheetAt(0);
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                if (sheet.GetRow(i) != null)
                {
                    //分提單號
                    trackingno = sheet.GetRow(i).GetCell(0) == null ? "" : sheet.GetRow(i).GetCell(0).ToString().Trim();
                    //讀到表頭 下一行開始讀取資料
                    if (sheet.GetRow(i).GetCell(0) != null && sheet.GetRow(i).GetCell(0).ToString().Trim() == "分提單號")
                    {
                        read = true;
                        continue;
                    }
                    if (read && trackingno != "")
                    {
                        dr = dt_Data.NewRow();
                        dr["TrackingNo"] = trackingno;
                        dt_Data.Rows.Add(dr);
                    }
                }
            }
            return dt_Data;
        }

        /// <summary>
        /// 取得轉檔紀錄
        /// </summary>
        /// <returns></returns>
        public DataTable GetLog_Work()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("select top 500 * from [jetf].[dbo].[LOG_WORK] order by StartTime desc ");
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), conn))
            {
                da.Fill(dt);
            }
            dt.Columns.Add("Format_StartTime", typeof(string));
            dt.Columns.Add("Format_EndTime", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i]["Format_StartTime"] = Convert.ToDateTime(dt.Rows[i]["StartTime"]).ToString("yyyy-MM-dd HH:mm:ss");
                dt.Rows[i]["Format_EndTime"] = Convert.ToDateTime(dt.Rows[i]["EndTime"]).ToString("yyyy-MM-dd HH:mm:ss");
            }
            return dt;
        }

        /// <summary>
        /// 取得稅單PDF路徑
        /// </summary>
        /// <param name="taxNumber"></param>
        /// <returns></returns>
        public string GetClearance_Tax_Pdf(string taxNumber)
        {
            string filePath = "";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT FilePath FROM [jetf].[dbo].[Clearance_Tax_Pdf] where TaxNumber=@TaxNumber ", conn))
            {
                da.SelectCommand.Parameters.Add("@TaxNumber", SqlDbType.NVarChar).Value = taxNumber;
                da.Fill(dt);
            }
            if (dt.Rows.Count > 0)
            {
                filePath = dt.Rows[0]["FilePath"].ToString();
            }
            dt.Dispose();
            return filePath;
        }

        /// <summary>
        /// 取得簽收單路徑
        /// </summary>
        /// <param name="cargoNumber"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public DataTable GetCargo_Sign_Receipt(string cargoNumber)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT FilePath FROM [jetf].[dbo].[Cargo_Sign_Receipt] where Jetf_Serial=@Jetf_Serial ", conn))
            {
                da.SelectCommand.Parameters.Add("@Jetf_Serial", SqlDbType.NVarChar).Value = cargoNumber;
                da.Fill(dt);
            }
            dt.Dispose();
            return dt;
        }
    }
}
