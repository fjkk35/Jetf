using Dapper;
using iTextSharp.text;
using Microsoft.VisualBasic.ApplicationServices;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Asn1.Ocsp;
using Service.Data;
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
using System.Data.Entity;
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
        // SeaClearanceListQueryItem moved to Service.Services.SeaClearance.Domain.SeaClearanceListQueryItem

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
            request = request ?? new SeaClearanceRequest();

            using (var db = CreateJetfDbContext())
            {
                var page = request.Page > 0 ? request.Page : 1;
                var pageSize = request.PageSize > 0 ? request.PageSize : 10;
                var query = BuildSeaClearanceListQuery(db, request);
                var totalCount = query.Count();
                var pageItems = query
                    .OrderBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                var data = MapSeaClearanceList(pageItems);

                return new SeaClearanceResponse
                {
                    TotalCount = totalCount,
                    Data = data
                };
            }
        }

        private IQueryable<SeaClearanceListQueryItem> BuildSeaClearanceListQuery(JetfDbContext db, SeaClearanceRequest request)
        {
            request = request ?? new SeaClearanceRequest();

            var trackingNo = string.IsNullOrWhiteSpace(request.TrackingNo) ? null : request.TrackingNo.Trim();
            var declNo = string.IsNullOrWhiteSpace(request.DeclNo) ? null : request.DeclNo.Trim();
            var custCode = string.IsNullOrWhiteSpace(request.CustCode) ? null : request.CustCode.Trim();
            var importer = string.IsNullOrWhiteSpace(request.Importer) ? null : request.Importer.Trim();
            var postEntry = request.PostEntry.HasValue ? request.PostEntry.ToDescription() : null;
            var warehouseType = request.Type.HasValue ? request.Type.ToDescription() : null;
            var detailIds = request.SeaClearanceDetailIds;

            var query =
                from detail in db.SeaClearanceDetails.AsNoTracking()
                let original = db.SeaClearanceDetailOriginalMappings
                    .Where(x => x.SeaClearanceDetailId == detail.Id && x.Gw.HasValue && x.Gw.Value > 0)
                    .OrderByDescending(x => x.Gw)
                    .ThenByDescending(x => x.SeaOrderOriginalId)
                    .FirstOrDefault()
                where detail.IsSucess && original != null
                select new SeaClearanceListQueryItem
                {
                    Id = detail.Id,
                    SeaClearanceId = detail.SeaClearanceId,
                    DataDate = detail.DataDate,
                    MainNumber = detail.MainNumber,
                    TrackingNo = detail.TrackingNo,
                    DeclNo = detail.DeclNo,
                    SignOutTime = detail.SignOutTime,
                    CreateDate = original.CreateDate,
                    Modifyby = original.Modifyby,
                    PostEntry = original.Post_Entry,
                    Eta = original.Eta,
                    CustCode = original.Cust_Code,
                    Piece = original.Piece,
                    Importer = original.Importer,
                    JetfSerial = original.Jetf_Serial,
                    ItemName = original.Item_Name,
                    CurrentStepId = detail.CurrentStepId,
                    CurrentAbnormalStateId = detail.CurrentAbnormalStateId
                };

            query = query.WhereIf(request.SeaClearanceId.HasValue, x => x.SeaClearanceId == request.SeaClearanceId.Value);
            query = query.WhereIf(request.SeaClearanceDetailId.HasValue, x => x.Id == request.SeaClearanceDetailId.Value);
            query = query.WhereIf(detailIds != null && detailIds.Any(), x => detailIds.Contains(x.Id));
            query = query.WhereIf(!string.IsNullOrWhiteSpace(trackingNo), x => x.TrackingNo == trackingNo);
            query = query.WhereIf(!string.IsNullOrWhiteSpace(declNo), x => x.DeclNo == declNo);
            query = query.WhereIf(!string.IsNullOrWhiteSpace(postEntry), x => x.PostEntry == postEntry);
            query = query.WhereIf(!string.IsNullOrWhiteSpace(custCode), x => x.CustCode == custCode);
            query = query.WhereIf(!string.IsNullOrWhiteSpace(importer), x => x.Importer == importer);
            query = query.WhereIf(!string.IsNullOrWhiteSpace(warehouseType), x => x.Modifyby == warehouseType);

            if (request.StepId.HasValue)
            {
                var stepId = request.StepId.Value;
                query = query.Where(x => x.CurrentStepId == stepId || (stepId == 2 && !x.CurrentStepId.HasValue));
            }

            query = query.WhereIf(request.AbnormalStateId.HasValue, x => x.CurrentAbnormalStateId == request.AbnormalStateId.Value);

            return query;
        }

        private List<int> GetFilteredSeaClearanceDetailIds(SeaClearanceRequest request)
        {
            using (var db = CreateJetfDbContext())
            {
                return BuildSeaClearanceListQuery(db, request)
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .ToList();
            }
        }

        private List<SeaClearanceModel> MapSeaClearanceList(List<SeaClearanceListQueryItem> items)
        {
            var stepNameMap = GetAllSteps()
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.Select(x => x.StepName).FirstOrDefault() ?? string.Empty);
            var customerNameMap = GetSeaCustomerNames(items.Select(x => x.CustCode));

            return items.Select(x => new SeaClearanceModel
            {
                Id = x.Id,
                DataDate = x.DataDate,
                MainNumber = x.MainNumber,
                TrackingNo = x.TrackingNo,
                DeclNo = x.DeclNo,
                CreateDate = x.CreateDate,
                Modifyby = x.Modifyby,
                Post_Entry = x.PostEntry,
                Eta = x.Eta,
                Cust_Code = x.CustCode,
                Cust_Name = !string.IsNullOrWhiteSpace(x.CustCode) && customerNameMap.TryGetValue(x.CustCode, out var customerName)
                    ? customerName
                    : string.Empty,
                Piece = x.Piece,
                Importer = x.Importer,
                Jetf_Serial = x.JetfSerial,
                Item_Name = x.ItemName,
                SignOutTime = x.SignOutTime,
                StepName = x.CurrentStepId.HasValue && stepNameMap.TryGetValue(x.CurrentStepId.Value, out var stepName)
                    ? stepName
                    : string.Empty
            }).ToList();
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
            request = request ?? new SeaClearanceRequest();

            if (request.SeaClearanceDetailIds != null && !request.SeaClearanceDetailIds.Any())
            {
                return new List<SeaClearanceDetailQueryModel>();
            }

            var trackingNo = string.IsNullOrWhiteSpace(request.TrackingNo) ? null : request.TrackingNo.Trim();
            var declNo = string.IsNullOrWhiteSpace(request.DeclNo) ? null : request.DeclNo.Trim();
            var custCode = string.IsNullOrWhiteSpace(request.CustCode) ? null : request.CustCode.Trim();
            var importer = string.IsNullOrWhiteSpace(request.Importer) ? null : request.Importer.Trim();
            var warehouseType = request.Type.HasValue ? request.Type.ToDescription() : null;
            var postEntry = request.PostEntry.HasValue ? request.PostEntry.ToDescription() : null;

            using (var db = CreateJetfDbContext())
            {
                IQueryable<SeaClearanceDetailEntity> detailQuery = db.SeaClearanceDetails
                    .AsNoTracking()
                    .Where(x => x.IsSucess);

                detailQuery = detailQuery.WhereIf(request.SeaClearanceId.HasValue, x => x.SeaClearanceId == request.SeaClearanceId.Value);
                detailQuery = detailQuery.WhereIf(request.SeaClearanceDetailId.HasValue, x => x.Id == request.SeaClearanceDetailId.Value);
                detailQuery = detailQuery.WhereIf(request.SeaClearanceDetailIds != null && request.SeaClearanceDetailIds.Any(), x => request.SeaClearanceDetailIds.Contains(x.Id));
                detailQuery = detailQuery.WhereIf(!string.IsNullOrWhiteSpace(trackingNo), x => x.TrackingNo == trackingNo);
                detailQuery = detailQuery.WhereIf(!string.IsNullOrWhiteSpace(declNo), x => x.DeclNo == declNo);

                IQueryable<SeaClearanceDetailOriginalMappingEntity> originalQuery = db.SeaClearanceDetailOriginalMappings.AsNoTracking();
                var hasOriginalFilter = false;

                if (!string.IsNullOrWhiteSpace(custCode))
                {
                    originalQuery = originalQuery.Where(x => x.Cust_Code == custCode);
                    hasOriginalFilter = true;
                }

                if (!string.IsNullOrWhiteSpace(importer))
                {
                    originalQuery = originalQuery.Where(x => x.Importer == importer);
                    hasOriginalFilter = true;
                }

                if (!string.IsNullOrWhiteSpace(warehouseType))
                {
                    originalQuery = originalQuery.Where(x => x.Modifyby == warehouseType);
                    hasOriginalFilter = true;
                }

                if (!string.IsNullOrWhiteSpace(postEntry))
                {
                    originalQuery = originalQuery.Where(x => x.Post_Entry == postEntry);
                    hasOriginalFilter = true;
                }

                if (hasOriginalFilter)
                {
                    var matchedDetailIds = originalQuery.Select(x => x.SeaClearanceDetailId);
                    detailQuery = detailQuery.Where(x => matchedDetailIds.Contains(x.Id));
                }

                var detailIds = detailQuery
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .ToList();

                if (!detailIds.Any())
                {
                    return new List<SeaClearanceDetailQueryModel>();
                }

                var list = detailQuery
                    .OrderBy(x => x.Id)
                    .Select(x => new SeaClearanceDetailQueryModel
                    {
                        Id = x.Id,
                        DataDate = x.DataDate,
                        MainNumber = x.MainNumber,
                        MftNo = x.MftNo,
                        TrackingNo = x.TrackingNo,
                        Memo = x.Memo,
                        ImportDate = x.ImportDate,
                        DeclNo = x.DeclNo,
                        ProDateTime = x.ProDateTime,
                        CrtDateTime = x.CrtDateTime ?? DateTime.MinValue,
                        IsSeaOrderOriginal = x.IsSeaOrderOriginal ?? false,
                        Tax = x.Tax,
                        CustomsBrokerId = x.CustomsBrokerId,
                        CustomsBrokerName = db.CustomsBrokers
                            .Where(y => y.Id == x.CustomsBrokerId)
                            .Select(y => y.Name)
                            .FirstOrDefault(),
                        CustomsBrokerageId = x.CustomsBrokerageId,
                        CustomsBrokerageName = db.CustomsBrokerages
                            .Where(y => y.Id == x.CustomsBrokerageId)
                            .Select(y => y.Name)
                            .FirstOrDefault(),
                        SignInTime = x.SignInTime,
                        SignOutTime = x.SignOutTime,
                        ContactEmail = x.ContactEmail,
                        ContactChangeData = x.ContactChangeData,
                        CurrentStepId = x.CurrentStepId,
                        CurrentAbnormalStateId = x.CurrentAbnormalStateId,
                        CurrentAbnormalStateName = db.AbnormalStates
                            .Where(y => y.Id == x.CurrentAbnormalStateId)
                            .Select(y => y.AbnormalStateName)
                            .FirstOrDefault(),
                        IsCustomsHold = x.IsCustomsHold ?? false,
                        CustomsHold = x.CustomsHold,
                        IsSucess = x.IsSucess
                    })
                    .ToList();

                var originalRows = db.SeaClearanceDetailOriginalMappings
                    .AsNoTracking()
                    .Where(x => detailIds.Contains(x.SeaClearanceDetailId))
                    .OrderBy(x => x.SeaClearanceDetailId)
                    .ThenByDescending(x => x.Gw)
                    .ThenByDescending(x => x.SeaOrderOriginalId)
                    .Select(x => new
                    {
                        x.SeaClearanceDetailId,
                        x.SeaOrderOriginalId,
                        x.CreateDate,
                        x.Modifyby,
                        x.Post_Entry,
                        x.Eta,
                        x.Cust_Code,
                        x.Piece,
                        x.Importer,
                        x.Im_Phoneno,
                        x.Importer_Id,
                        x.CC,
                        x.Tax_Payment,
                        x.Item_Name,
                        x.Jetf_Serial,
                        x.Gw
                    })
                    .ToList();

                var customerNameMap = GetSeaCustomerNames(originalRows.Select(x => x.Cust_Code));

                var feeCustCodes = originalRows
                    .Select(x => x.Cust_Code)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                var feeMap = db.SeaClearanceFees
                    .AsNoTracking()
                    .Where(x => feeCustCodes.Contains(x.CustCode))
                    .GroupBy(x => x.CustCode)
                    .ToDictionary(g => g.Key, g => g.FirstOrDefault());

                var originalsByDetailId = originalRows
                    .Select(x =>
                    {
                        feeMap.TryGetValue(x.Cust_Code ?? string.Empty, out var fee);
                        return new SeaOrderOriginalModel
                        {
                            SeaClearanceDetailId = x.SeaClearanceDetailId,
                            SeaOrderOriginalId = x.SeaOrderOriginalId,
                            CreateDate = x.CreateDate,
                            Modifyby = x.Modifyby,
                            Post_Entry = x.Post_Entry,
                            Eta = x.Eta,
                            Cust_Code = x.Cust_Code,
                            Cust_Name = !string.IsNullOrWhiteSpace(x.Cust_Code) && customerNameMap.TryGetValue(x.Cust_Code, out var custName)
                                ? custName
                                : string.Empty,
                            Piece = x.Piece,
                            Importer = x.Importer,
                            Im_Phoneno = x.Im_Phoneno,
                            Importer_Id = x.Importer_Id,
                            CC = x.CC,
                            Tax_Payment = x.Tax_Payment,
                            Jetf_Serial = x.Jetf_Serial,
                            Item_Name = x.Item_Name,
                            Gw = x.Gw ?? 0,
                            G1Fee = fee != null ? (int?)fee.G1Fee : null,
                            MoveWarehouseFee = fee != null ? (int?)fee.MoveWarehouseFee : null,
                            TransferG1Fee = fee != null ? (int?)fee.TransferG1Fee : null,
                            TransferWarehouseFee = fee != null ? (int?)fee.TransferWarehouseFee : null,
                            X2Fee = fee != null ? (int?)fee.X2Fee : null
                        };
                    })
                    .GroupBy(x => x.SeaClearanceDetailId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var detail in list)
                {
                    detail.SeaOrderOriginals = originalsByDetailId.TryGetValue(detail.Id, out var originals)
                        ? originals
                        : new List<SeaOrderOriginalModel>();
                }

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
                        var startDate = item.SignInTime.Value.Date;
                        var endDate = item.SignOutTime.HasValue ? item.SignOutTime.Value.Date : DateTime.Now.Date;
                        var days = (endDate - startDate).Days + 1;
                        item.WarehouseDays = days > 0 ? days : 0;
                    }
                    else
                    {
                        item.WarehouseDays = 0;
                    }

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
                            }
                        }
                    }
                    else
                    {
                        var seaClearanceCustTaxPayment = seaClearanceCustTaxPayments.FirstOrDefault(x => x.CustCode == seaOrderOriginal?.Cust_Code);
                        if (seaClearanceCustTaxPayment != null)
                        {
                            seaOrderOriginal.Tax_Payment = seaClearanceCustTaxPayment.TaxPayment.ToDescription();
                        }
                    }
                }

                return list;
            }
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
