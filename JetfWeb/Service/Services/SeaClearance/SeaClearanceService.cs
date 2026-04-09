using Dapper;
using iTextSharp.text;
using Microsoft.VisualBasic.ApplicationServices;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Asn1.Ocsp;
using Service.EnumTax;
using Service.Extensions;
using Service.Helpers;
using Service.Models;
using Service.Models.CptTradeVan;
using Service.Models.SeaClearance;
using Service.Models.SeaClearanceCreate;
using Service.Models.SeaClearanceCustTaxPayment;
using Service.Models.SeaClearanceSjlTaxPayment;
using Service.Services.CptTradeVan;
using Service.Services.CustomsBroker;
using Service.Services.SeaClearance.Domain;
using Service.Services.SeaClearanceDetailEditHistory;
using Service.Services.ShipmentInboundProcess.Domain;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI;
using TelegramLibrary.Model;

namespace Service.Services.SeaClearance
{
    public partial class SeaClearanceService : _BaseService
    {
        private readonly SeaClearanceDetailEditHistoryService _editHistoryService;
        private readonly CustomsBrokerService _customsBrokerService;
        private readonly CptPortalApi _cptPortalApi;

        public SeaClearanceService(CustomsBrokerService customsBrokerService,
            SeaClearanceDetailEditHistoryService editHistoryService,
            CptPortalApi cptPortalApi)
        {
            _editHistoryService = editHistoryService;
            _customsBrokerService = customsBrokerService;
            _cptPortalApi = cptPortalApi;
        }

        public SeaClearanceResponse GetData(SeaClearanceRequest request)
        {
            var parameters = new DynamicParameters();

            var mainSql = @"
                 SELECT COUNT(*) FROM (
                        {0}
                    ) AS Filtered;
                
                SELECT * FROM (
                       {0}
                   ) AS Filtered
                ORDER BY Id  
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY
            ";

            //SQL 查詢
            var sql = @"
                SELECT 
                a.Id,a.DataDate,a.MainNumber,a.TrackingNo,a.DeclNo,a.SignOutTime,
				c.CreateDate, c.Modifyby, c.Post_Entry, c.Eta, c.Cust_Code,d.Cust_Name, c.Piece, c.Importer,c.Jetf_Serial,
				c.Item_Name,
                f.StepName
				FROM [jetf].[dbo].[SeaClearanceDetail] a 
                LEFT JOIN [jetf].[dbo].SeaClearanceDetailOriginalMapping c ON a.Id = c.SeaClearanceDetailId
                LEFT JOIN [DATA_CENTER].[dbo].[SYS_CUST] d ON c.Cust_Code = d.CUST_CODE
                LEFT JOIN [jetf].[dbo].[SeaClearanceFee] e ON c.Cust_Code = e.CustCode
				LEFT JOIN [jetf].[dbo].[Step] f ON a.CurrentStepId = f.Id
                where IsSucess = '1' and c.Gw > 0 
            ";

            //分提單號
            if (!string.IsNullOrEmpty(request.TrackingNo))
            {
                sql += " and a.TrackingNo = @TrackingNo";
                parameters.Add("TrackingNo", request.TrackingNo);
            }

            //報單號碼
            if (!string.IsNullOrEmpty(request.DeclNo))
            {
                sql += " and a.DeclNo = @DeclNo";
                parameters.Add("DeclNo", request.DeclNo);
            }

            //報關方式
            if (request.PostEntry.HasValue)
            {
                sql += " and c.Post_Entry = @PostEntry";
                parameters.Add("PostEntry", request.PostEntry.ToDescription());
            }

            //客戶
            if (!string.IsNullOrEmpty(request.CustCode))
            {
                sql += " and c.Cust_Code = @CustCode";
                parameters.Add("CustCode", request.CustCode);
            }

            //原單申報人
            if (!string.IsNullOrEmpty(request.Importer))
            {
                sql += " and c.Importer = @Importer";
                parameters.Add("Importer", request.Importer);
            }

            //倉別
            if (request.Type.HasValue)
            {
                sql += " and c.Modifyby = @Type";
                parameters.Add("Type", request.Type.ToDescription());
            }

            //步驟
            if (request.StepId.HasValue)
            {
                sql += " and (a.CurrentStepId = @StepId OR (@StepId = 2 AND a.CurrentStepId IS NULL))";
                parameters.Add("StepId", request.StepId.Value);
            }

            //異常狀態
            if (request.AbnormalStateId.HasValue)
            {
                sql += " and a.CurrentAbnormalStateId = @AbnormalStateId";
                parameters.Add("AbnormalStateId", request.AbnormalStateId.Value);
            }

            mainSql = string.Format(mainSql, sql);

            parameters.Add("Offset", (request.Page - 1) * request.PageSize);
            parameters.Add("PageSize", request.PageSize);

            using (var query = conn.QueryMultiple(mainSql, parameters))
            {
                var totalCount = query.ReadFirst<int>();
                var data = query.Read<SeaClearanceModel>().ToList();

                return new SeaClearanceResponse
                {
                    TotalCount = totalCount,
                    Data = data
                };
            }
        }

        public SeaClearanceDetailQueryModel GetDetail(int id)
        {
            var request = new SeaClearanceRequest
            {
                SeaClearanceDetailId = id
            };

            var detail = GetSeaClearance(request).FirstOrDefault();

            return detail;
        }

        public List<SelectListModel> GetCustomsBrokerageOptions()
        {
            const string sql = @"
                SELECT CAST(Id AS VARCHAR(20)) AS Value, Name AS Text
                FROM jetf.dbo.CustomsBrokerage
                ORDER BY Name
            ";

            return conn.Query<SelectListModel>(sql).ToList();
        }

        /// <summary>
        /// 更新入倉與出倉時間
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ResponseModel UpdateSignInOutTime(int id)
        {
            try
            {
                var detail = GetDetail(id);
                
                if (!CanUpdateSignInOutTime(detail))
                {
                    return new ResponseModel
                    {
                        ReturnObject = new
                        {
                            SignInTime = detail?.SignInTime,
                            SignOutTime = detail?.SignOutTime,
                            Updated = false
                        }
                    };
                }

                var sql = @"
                    select 
                    SIGN_IN_TIME as SignInTime,
                    SIGN_OUT_TIME as SignOutTime
                    from DATA_CENTER.[dbo].[CLEARANCE_INFO]
                    where MAIN_NUMBER=@MainNumber and MERGE_NUMBER=@TrackingNo";

                var clearanceInfo = conn.QueryFirstOrDefault<ClearanceSignTimeModel>(sql, new
                {
                    MainNumber = detail.MainNumber,
                    TrackingNo = detail.TrackingNo
                });

                if (clearanceInfo == null)
                {
                    return new ResponseModel
                    {
                        ReturnObject = new
                        {
                            SignInTime = detail.SignInTime,
                            SignOutTime = detail.SignOutTime,
                            Updated = false
                        }
                    };
                }

                var newSignInTime = detail.SignInTime ?? clearanceInfo.SignInTime;
                var newSignOutTime = detail.SignOutTime ?? clearanceInfo.SignOutTime;

                if (newSignInTime == detail.SignInTime && newSignOutTime == detail.SignOutTime)
                {
                    return new ResponseModel
                    {
                        ReturnObject = new
                        {
                            SignInTime = detail.SignInTime,
                            SignOutTime = detail.SignOutTime,
                            Updated = false
                        }
                    };
                }

                var updateSql = @"
                    UPDATE jetf.dbo.SeaClearanceDetail 
                    SET 
                    SignInTime = CASE WHEN SignInTime IS NULL THEN @SignInTime ELSE SignInTime END,
                    SignOutTime = CASE WHEN SignOutTime IS NULL THEN @SignOutTime ELSE SignOutTime END
                    WHERE Id = @Id";

                conn.Execute(updateSql, new
                {
                    Id = detail.Id,
                    SignInTime = newSignInTime,
                    SignOutTime = newSignOutTime
                });

                return new ResponseModel
                {
                    ReturnObject = new
                    {
                        SignInTime = newSignInTime,
                        SignOutTime = newSignOutTime,
                        Updated = true
                    }
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        private bool CanUpdateSignInOutTime(SeaClearanceDetailQueryModel detail)
        {
            if (detail == null)
            {
                return false;
            }

            if (!IsPostEntryEligible(detail))
            {
                return false;
            }

            if (detail.SignInTime.HasValue && detail.SignOutTime.HasValue)
            {
                return false;
            }

            return !string.IsNullOrEmpty(detail.MainNumber) && !string.IsNullOrEmpty(detail.TrackingNo);
        }

        private bool IsPostEntryEligible(SeaClearanceDetailQueryModel detail)
        {
            var postEntry = detail.SeaOrderOriginals?.FirstOrDefault()?.Post_Entry?.Trim();
            if (string.IsNullOrEmpty(postEntry))
            {
                return false;
            }

            var postEntryCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "X2",
                "X3",
                "G1",
                "轉G1"
            };

            return postEntryCandidates.Contains(postEntry);
        }

        /// <summary>
        /// 取得明細資料
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SeaClearanceDetailModel GetSeaClearanceDetailById(int id)
        {
            var request = new SeaClearanceRequest
            {
                SeaClearanceDetailId = id
            };
            var sql = @"
                select a.ProDateTime, a.DeclNo, b.Post_Entry from [jetf].[dbo].[SeaClearanceDetail] a
                left join [jetf].[dbo].[SeaClearanceDetailOriginalMapping] b on a.Id=b.SeaClearanceDetailId and b.GW > 0
                where a.IsSucess = '1' and a.Id = @SeaClearanceDetailId 
            ";

            return conn.QueryFirstOrDefault<SeaClearanceDetailModel>(sql, new
            {
                SeaClearanceDetailId = request.SeaClearanceDetailId
            });
        }

        /// <summary>
        /// 更新欄位的通用方法
        /// </summary>
        /// <param name="id">明細ID</param>
        /// <param name="field">欄位類型</param>
        /// <param name="value">新值</param>
        /// <param name="userId">使用者ID</param>
        /// <returns></returns>
        public ResponseModel UpdateField(int id, SeaClearanceEditField field, string newValue)
        {
            try
            {
                // 取得目前資料
                var currentData = GetDetail(id);
                if (currentData == null)
                {
                    return new ResponseModel("找不到指定資料");
                }

                string columnName = field.ToString();

                if (string.IsNullOrEmpty(columnName))
                {
                    return new ResponseModel("無效的欄位名稱");
                }

                string sql = string.Empty;
                switch (field)
                {
                    case SeaClearanceEditField.CustomsBrokerId:
                    case SeaClearanceEditField.CustomsBrokerageId:
                    case SeaClearanceEditField.SignInTime:
                    case SeaClearanceEditField.SignOutTime:
                    case SeaClearanceEditField.ContactEmail:
                    case SeaClearanceEditField.ContactChangeData:
                    case SeaClearanceEditField.DeclNo:
                    case SeaClearanceEditField.IsCustomsHold:
                    case SeaClearanceEditField.CustomsHold:
                        // 這些欄位在 SeaClearanceDetail 表
                        sql = $@"
                            UPDATE jetf.dbo.SeaClearanceDetail 
                            SET {columnName} = @NewValue 
                            WHERE Id = @Id
                        ";

                        conn.Execute(sql, new
                        {
                            Id = id,
                            NewValue = newValue
                        });
                        break;
                    case SeaClearanceEditField.Post_Entry:
                    case SeaClearanceEditField.Importer_Id:
                    case SeaClearanceEditField.Importer:
                        // 這些欄位在 SeaClearanceDetailOriginalMapping 表
                        sql = $@"
                            UPDATE jetf.dbo.SeaClearanceDetailOriginalMapping 
                            SET {columnName} = @NewValue 
                            WHERE SeaClearanceDetailId = @Id
                        ";

                        conn.Execute(sql, new
                        {
                            Id = id,
                            NewValue = newValue
                        });
                        break;
                    default:
                        return new ResponseModel("無效的欄位名稱");
                }

                //新增編輯紀錄
                _editHistoryService.RecordEdit(
                   id,
                   field,
                   currentData,
                   newValue
                );

                return new ResponseModel();
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 更新到港日
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public ResponseModel UpdateEta(int seaClearanceDetailId)
        {
            try
            {
                var detail = GetDetail(seaClearanceDetailId);
                if (detail == null)
                {
                    return new ResponseModel("找不到指定資料");
                }

                if (string.IsNullOrEmpty(detail.MainNumber) || string.IsNullOrEmpty(detail.TrackingNo))
                {
                    return new ResponseModel("主號或分提單號碼為空");
                }

                var sql = @"
                    select top 1 
                    ETA as Eta
                    from [DATA_CENTER].[dbo].[SEA_ORDER_ORIGINAL]
                    where MAINNUMBER=@MainNumber 
                    and BL_NO=@TrackingNo
                    order by GW desc";

                var eta = conn.QueryFirstOrDefault<DateTime?>(sql, new
                {
                    MainNumber = detail.MainNumber,
                    TrackingNo = detail.TrackingNo
                });

                if (!eta.HasValue)
                {
                    return new ResponseModel("查無預計到港日");
                }

                var seaOrderOriginals = detail.SeaOrderOriginals?.FirstOrDefault(r => r.Gw > 0);

                //到港日一樣不用更新
                if (seaOrderOriginals?.Eta == eta)
                {
                    return new ResponseModel
                    {
                        ReturnObject = eta
                    };
                }

                var updateSql = @"
                    update [jetf].[dbo].[SeaClearanceDetailOriginalMapping] 
                    set Eta=@Eta
                    where SeaClearanceDetailId=@SeaClearanceDetailId";

                conn.Execute(updateSql, new
                {
                    SeaClearanceDetailId = seaClearanceDetailId,
                    Eta = eta.Value.ToString("yyyy-MM-dd")
                });

                return new ResponseModel
                {
                    ReturnObject = eta
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        ///更新報關資料到港日
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <returns></returns>
        public ResponseModel UpdateImportDate(int seaClearanceDetailId)
        {
            try
            {
                var detail = GetDetail(seaClearanceDetailId);
                if (detail == null)
                {
                    return new ResponseModel("找不到指定資料");
                }

                // 先確認掛號是否存在；若明細本身沒有，則嘗試依主號回查並回寫資料表。
                var mftNo = GetMftNo(detail);
                if (string.IsNullOrWhiteSpace(mftNo))
                {
                    return new ResponseModel("掛號為空");
                }

                // 取得掛號後再呼叫 CPT 查詢最新艙單到港日。
                var parameters = new Dictionary<string, string>
                {
                    { "tab1.currentPage", "1" },
                    { "tab1.rowNum", "10" },
                    { "tab1.hideDeclNo", "" },
                    { "tab1.vslRegNo", mftNo },
                    { "tab1.mftNo", "" },
                    { "choice", "B" },
                    { "tab1.mawb", detail.MainNumber },
                    { "tab1.hawb", detail.TrackingNo }
                };

                var result = _cptPortalApi.GetGb326(parameters);
                var importDate = result?.ImportDate;

                if (string.IsNullOrEmpty(importDate))
                {
                    return new ResponseModel("查無艙單到港日");
                }

                // 艙單到港日相同時不重複更新，只回傳目前畫面需要的資料。
                if (detail.ImportDate == importDate)
                {
                    return new ResponseModel
                    {
                        ReturnObject = new
                        {
                            ImportDate = importDate,
                            CustomerDeadline = detail.CustomerDeadline,
                            CloseDate = detail.CloseDate,
                            ProDateTimeDeadline = detail.ProDateTimeDeadline,
                            LateDeclarationFee = detail.LateDeclarationFee
                        }
                    };
                }

                // 寫回最新艙單到港日後，重新計算相關截止日與滯報費。
                var updateSql = @"
                    update [jetf].[dbo].SeaClearanceDetail 
                    set ImportDate=@ImportDate
                    where Id=@Id";

                conn.Execute(updateSql, new
                {
                    Id = seaClearanceDetailId,
                    ImportDate = importDate
                });

                detail.ImportDate = importDate;
                CalculateDeadlines(detail);

                return new ResponseModel
                {
                    ReturnObject = new
                    {
                        ImportDate = importDate,
                        CustomerDeadline = detail.CustomerDeadline,
                        CloseDate = detail.CloseDate,
                        ProDateTimeDeadline = detail.ProDateTimeDeadline,
                        LateDeclarationFee = detail.LateDeclarationFee
                    }
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel(ex.Message);
            }
        }

        /// <summary>
        /// 確保明細有可用掛號；若明細尚未帶入，則依主號回查並回寫資料表。
        /// </summary>
        /// <returns>可用掛號；若查無資料則回傳 null。</returns>
        private string GetMftNo(SeaClearanceDetailQueryModel detail)
        {
            if (!string.IsNullOrWhiteSpace(detail.MftNo))
            {
                return detail.MftNo;
            }

            // 明細沒有掛號時，改從與法主檔依主號回查第一筆掛號。
            var mftNo = conn.QueryFirstOrDefault<string>(@"
                select top 1 FIELD_A as MftNo
                from DATA_CENTER.dbo.CES_MAIN_ORDER
                where MAIN_NUMBER = @MainNumber
            ", new
            {
                MainNumber = detail.MainNumber
            });

            if (string.IsNullOrWhiteSpace(mftNo))
            {
                return null;
            }

            // 查到掛號後同步回寫 SeaClearanceDetail，避免後續流程重複查詢。
            conn.Execute(@"
                update [jetf].[dbo].[SeaClearanceDetail]
                set MftNo = @MftNo
                where Id = @Id
            ", new
            {
                Id = detail.Id,
                MftNo = mftNo
            });

            detail.MftNo = mftNo;

            return mftNo;
        }

        /// <summary>
        /// 計算報單傳輸截止日、要求客戶截止日、強制結案日
        /// </summary>
        /// <param name="item"></param>
        private void CalculateDeadlines(SeaClearanceDetailQueryModel item)
        {
            var importDate = item.ImportDate.ToDateTime("yyyyMMdd");
            var seaOrderOriginal = item.SeaOrderOriginals?.FirstOrDefault(x => x.Gw > 0);

            item.CustomerDeadline = importDate?.AddDays(5);
            item.CloseDate = importDate?.AddDays(90);
            item.ProDateTimeDeadline = importDate?.AddDays(15);

            if (item.ProDateTime.HasValue && item.ProDateTimeDeadline.HasValue)
            {
                var days = (item.ProDateTime.Value - item.ProDateTimeDeadline.Value).Days;
                item.LateDeclarationFee = days * 200;
            }
            else if (!item.ProDateTime.HasValue && item.ProDateTimeDeadline.HasValue)
            {
                var days = (DateTime.Now - item.ProDateTimeDeadline.Value).Days;
                item.LateDeclarationFee = days * 200;
            }

            var postEntry = seaOrderOriginal?.Post_Entry;
            if (postEntry == "轉移倉" || postEntry == "轉G1")
            {
                item.LateDeclarationFee = 0;
            }
        }

        /// <summary>
        /// 下載明細資料
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        List<SeaClearanceDetailQueryModel> GetSeaClearance(SeaClearanceRequest request)
        {
            var parameters = new DynamicParameters();

            var sql = @"
                select 
                    a.Id, a.DataDate, a.MainNumber, a.MftNo, a.TrackingNo, a.Memo, 
                    a.ImportDate, a.DeclNo, a.ProDateTime, a.CrtDateTime, a.IsSeaOrderOriginal, a.Tax,
                    a.CustomsBrokerId, f.Name as CustomsBrokerName,
                    a.CustomsBrokerageId, g.Name as CustomsBrokerageName,
                    a.SignInTime, a.SignOutTime,
                    a.ContactEmail, a.ContactChangeData,
                    a.CurrentStepId,
                    a.CurrentAbnormalStateId,
                    h.AbnormalStateName as CurrentAbnormalStateName,
                    a.IsCustomsHold, a.CustomsHold,
                    --a.InspectionType, a.ProcessingPersonnel, a.ReceivedOriginalMenu, 
                    --a.DocumentDeliveryMenu, a.ContactChangeData, a.ContactEmail,
                    -- SeaOrderOriginal 相關欄位
                    c.SeaClearanceDetailId, c.CreateDate, c.Modifyby, c.Post_Entry, c.Eta, 
                    c.Cust_Code, d.Cust_Name, c.Piece, c.Importer, c.Im_Phoneno,c.Importer_Id, c.CC, 
                    c.Tax_Payment, c.Jetf_Serial, c.Gw,
                    -- Fee 相關欄位
                    e.G1Fee, e.MoveWarehouseFee, e.TransferG1Fee, e.TransferWarehouseFee, e.X2Fee
                from [jetf].[dbo].[SeaClearanceDetail] a 
                left join [jetf].[dbo].SeaClearanceDetailOriginalMapping c on a.Id = c.SeaClearanceDetailId
                left join [DATA_CENTER].[dbo].[SYS_CUST] d on c.Cust_Code = d.CUST_CODE
                left join [jetf].[dbo].[SeaClearanceFee] e on c.Cust_Code = e.CustCode
                left join [jetf].[dbo].[CustomsBroker] f on a.CustomsBrokerId = f.Id
                left join [jetf].[dbo].[CustomsBrokerage] g on a.CustomsBrokerageId = g.Id
                left join [jetf].[dbo].[AbnormalState] h on a.CurrentAbnormalStateId = h.Id
                where a.IsSucess = '1'
            ";

            //上傳Id
            if (request.SeaClearanceId.HasValue)
            {
                sql += " and a.SeaClearanceId = @SeaClearanceId";
                parameters.Add("SeaClearanceId", request.SeaClearanceId.Value);
            }

            //明細Id
            if (request.SeaClearanceDetailId.HasValue)
            {
                sql += " and a.Id = @SeaClearanceDetailId";
                parameters.Add("SeaClearanceDetailId", request.SeaClearanceDetailId.Value);
            }

            //分提單號
            if (!string.IsNullOrEmpty(request.TrackingNo))
            {
                sql += " and a.TrackingNo = @TrackingNo";
                parameters.Add("TrackingNo", request.TrackingNo);
            }

            //報單號碼
            if (!string.IsNullOrEmpty(request.DeclNo))
            {
                sql += " and a.DeclNo = @DeclNo";
                parameters.Add("DeclNo", request.DeclNo);
            }

            //客戶
            if (!string.IsNullOrEmpty(request.CustCode))
            {
                sql += " and c.Cust_Code = @CustCode";
                parameters.Add("CustCode", request.CustCode);
            }

            //原單申報人
            if (!string.IsNullOrEmpty(request.Importer))
            {
                sql += " and c.Importer = @Importer";
                parameters.Add("Importer", request.Importer);
            }

            //倉別
            if (request.Type.HasValue)
            {
                sql += " and c.Modifyby = @Type";
                parameters.Add("Type", request.Type.ToDescription());
            }

            //報關方式
            if (request.PostEntry.HasValue)
            {
                sql += " and c.Post_Entry = @PostEntry";
                parameters.Add("PostEntry", request.PostEntry.ToDescription());
            }

            var queryResult = conn.Query<SeaClearanceDetailQueryModel, SeaOrderOriginalModel, (SeaClearanceDetailQueryModel detail, SeaOrderOriginalModel original)>(sql,
                (detail, original) => (detail, original),
                parameters,
                splitOn: "SeaClearanceDetailId").ToList();

            //GroupBY
            var list = queryResult
                .GroupBy(x => x.detail.Id)
                .Select(g =>
                {
                    var detail = g.First().detail;
                    detail.SeaOrderOriginals = g
                        .Where(x => x.original != null && x.original.SeaClearanceDetailId > 0)
                        .Select(x => x.original)
                        .OrderByDescending(x => x.Gw)
                        .ToList();
                    return detail;
                })
                .ToList();

            //捷利原單申報人收費方式
            var sjlTaxPayments = GetSeaClearanceSjlTaxPayment();
            //客戶收費方式
            var seaClearanceCustTaxPayments = GetSeaClearanceCustTaxPayment();

            foreach (var item in list)
            {
                CalculateDeadlines(item);

                var seaOrderOriginal = item.SeaOrderOriginals.FirstOrDefault(x => x.Gw > 0);

                //到倉天數，倉日期-入倉日期+1
                //若未有出倉日期: 今日日期(查的當天)-入倉日期+1
                //出倉、入倉都未有值代0
                if (item.SignInTime.HasValue)
                {
                    // 只取日期部分，包含當日所以 +1
                    var startDate = item.SignInTime.Value.Date;
                    var endDate = item.SignOutTime.HasValue ? item.SignOutTime.Value.Date : DateTime.Now.Date;

                    var days = (endDate - startDate).Days + 1;

                    // 若計算結果為負，改為 0（資料異常安全處理）
                    item.WarehouseDays = days > 0 ? days : 0;
                }
                else
                {
                    // 未有入倉日期時回傳 0
                    item.WarehouseDays = 0;
                }


                //報關費用1
                switch (seaOrderOriginal?.Post_Entry)
                {
                    case "G1":
                        item.ClearanceFee = seaOrderOriginal?.G1Fee ?? 0;
                        break;
                    case "轉G1":
                        item.ClearanceFee = seaOrderOriginal?.TransferG1Fee ?? 0;
                        break;
                    case "移倉":
                        item.ClearanceFee = seaOrderOriginal?.MoveWarehouseFee ?? 0;
                        break;
                    case "轉移倉":
                        item.ClearanceFee = seaOrderOriginal?.MoveWarehouseFee ?? 0;
                        break;
                    case "X2":
                    case "X3":
                        item.ClearanceFee = seaOrderOriginal?.X2Fee ?? 0;
                        break;
                    default:
                        item.ClearanceFee = 0;
                        break;
                }

                //收費方式
                if (seaOrderOriginal?.Cust_Name == "捷利")
                {
                    var sjlTaxPayment = sjlTaxPayments.FirstOrDefault(x => x.Importer == seaOrderOriginal.Importer);
                    if (sjlTaxPayment != null)
                    {
                        seaOrderOriginal.Tax_Payment = sjlTaxPayment.TaxPayment.ToDescription();
                    }
                    else
                    {
                        switch (seaOrderOriginal?.Tax_Payment)
                        {
                            case "P":
                                seaOrderOriginal.Tax_Payment = "客戶";
                                break;
                            case "C":
                            case "Y":
                                seaOrderOriginal.Tax_Payment = "代收";
                                break;
                                //case "D":
                                //    seaOrderOriginal.Tax_Payment = "匯款";
                                //    break;
                        }
                    }
                }
                else
                {
                    //客戶收費方式
                    var seaClearanceCustTaxPayment = seaClearanceCustTaxPayments.FirstOrDefault(x => x.CustCode == seaOrderOriginal?.Cust_Code);
                    if (seaClearanceCustTaxPayment != null)
                    {
                        seaOrderOriginal.Tax_Payment = seaClearanceCustTaxPayment.TaxPayment.ToDescription();
                    }
                }
            }

            return list;
        }

        /// <summary>
        ///取得捷利客戶收費方式
        /// </summary>
        /// <returns></returns>
        private List<SeaClearanceSjlTaxPaymentModel> GetSeaClearanceSjlTaxPayment()
        {
            var sqlQuery = "SELECT * FROM jetf.dbo.SeaClearanceSjlTaxPayment";

            return conn.Query<SeaClearanceSjlTaxPaymentModel>(sqlQuery).ToList();
        }

        /// <summary>
        /// 取得客戶收費方式
        /// </summary>
        /// <returns></returns>
        private List<SeaClearanceCustTaxPaymentModel> GetSeaClearanceCustTaxPayment()
        {
            var sqlQuery = "SELECT * FROM jetf.dbo.SeaClearanceCustTaxPayment";

            return conn.Query<SeaClearanceCustTaxPaymentModel>(sqlQuery).ToList();
        }

        /// <summary>
        ///  取得海運客戶
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SelectListItem> GetSeaCustomerList()
        {
            var sql = @"
                        select Cust_Code,Cust_Name from jetf.dbo.SeaClearanceCustomer
                        order by Cust_Code
                        ";

            var list = conn.Query(sql).Select(item => new SelectListItem
            {
                Value = item.Cust_Code,
                Text = $"{item.Cust_Code}-{item.Cust_Name}"
            });

            return list;
        }

    }
}
