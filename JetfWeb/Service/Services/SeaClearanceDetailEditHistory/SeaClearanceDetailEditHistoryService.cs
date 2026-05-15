using Dapper;
using Service.EnumTax;
using Service.Extensions;
using Service.Models.CptTradeVan;
using Service.Models.SeaClearanceCreate;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.SeaClearanceDetailEditHistory
{
    public class SeaClearanceDetailEditHistoryService : _BaseService
    {
        public SeaClearanceDetailEditHistoryService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 記錄編輯歷史的通用函式
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="field"></param>
        /// <param name="data"></param>
        /// <param name="newValue"></param>
        /// <param name="userId"></param>
        public void RecordEdit(int seaClearanceDetailId, SeaClearanceEditField field, SeaClearanceDetailQueryModel data, string newValue)
        {
            var sql = @"
                INSERT INTO jetf.dbo.SeaClearanceDetailEditHistory 
                (SeaClearanceDetailId, FieldName, OldValue, NewValue, EditTime, EditUser)
                VALUES 
                (@SeaClearanceDetailId, @FieldName, @OldValue, @NewValue, @EditTime, @EditUser)
            ";

            var oldName = GetCurrentFieldValue(data, field);
            var newName = GetNewFieldValue(field, newValue);

            //如果新舊值相同，則不記錄
            if (oldName == newName)
                return;

            conn.Execute(sql, new
            {
                SeaClearanceDetailId = seaClearanceDetailId,
                FieldName = field.ToDescription(),
                OldValue = oldName,
                NewValue = newName,
                EditTime = DateTime.Now,
                EditUser = GetUserId()
            });
        }

        public void RecordEdit(
            SqlTransaction transaction,
            SqlConnection cn,
            int seaClearanceDetailId,
            SeaClearanceEditField field,
            string newValue,
            string memo,
            string user = null
            )
        {
            var sql = @"
                INSERT INTO jetf.dbo.SeaClearanceDetailEditHistory 
                (SeaClearanceDetailId, FieldName, NewValue, Memo, EditTime, EditUser)
                VALUES 
                (@SeaClearanceDetailId, @FieldName, @NewValue, @Memo, @EditTime, @EditUser)
            ";

            user = string.IsNullOrEmpty(user) ? GetUserId() : user;

            cn.Execute(sql, new
            {
                SeaClearanceDetailId = seaClearanceDetailId,
                FieldName = field.ToDescription(),
                NewValue = newValue,
                Memo = memo,
                EditTime = DateTime.Now,
                EditUser = user
            }, transaction);
        }

        /// <summary>
        ///取得目前欄位值
        /// </summary>
        private string GetCurrentFieldValue(SeaClearanceDetailQueryModel data, SeaClearanceEditField field)
        {
            switch (field)
            {
                case SeaClearanceEditField.CustomsBrokerId:
                    return data.CustomsBrokerName;
                case SeaClearanceEditField.CustomsBrokerageId:
                    return data.CustomsBrokerageName;
                case SeaClearanceEditField.SignInTime:
                    return data.SignInTime?.ToString("yyyy/MM/dd") ?? "";
                case SeaClearanceEditField.SignOutTime:
                    return data.SignOutTime?.ToString("yyyy/MM/dd") ?? "";
                case SeaClearanceEditField.ContactEmail:
                    return data.ContactEmail;
                case SeaClearanceEditField.ContactChangeData:
                    return data.ContactChangeData;
                case SeaClearanceEditField.DeclNo:
                    return data.DeclNo;
                case SeaClearanceEditField.Importer:
                    return data.SeaOrderOriginals.FirstOrDefault()?.Importer;
                case SeaClearanceEditField.Importer_Id:
                    return data.SeaOrderOriginals.FirstOrDefault()?.Importer_Id;
                case SeaClearanceEditField.Post_Entry:
                    return data.SeaOrderOriginals.FirstOrDefault()?.Post_Entry;
                case SeaClearanceEditField.IsCustomsHold:
                    return data.IsCustomsHold ? "是" : "否";
                case SeaClearanceEditField.CustomsHold:
                    return data.CustomsHold;
                default:
                    return "";
            }
        }

        /// <summary>
        /// 取得新欄位值
        /// </summary>
        private string GetNewFieldValue(SeaClearanceEditField field, string newValue)
        {
            switch (field)
            {
                case SeaClearanceEditField.CustomsBrokerId:
                    return GetCustomsBrokerName(newValue.ToInt());
                case SeaClearanceEditField.CustomsBrokerageId:
                    return GetCustomsBrokerageName(newValue.ToInt());
                case SeaClearanceEditField.IsCustomsHold:
                    return newValue == "true" ? "是" : "否";
                default:
                    return newValue;
            }
        }

        private string GetCustomsBrokerName(int id)
        {
            var sql = @"select Name from [jetf].[dbo].[CustomsBroker]
                    where Id=@Id and IsDelete = 0";

            return conn.Query<string>(sql, new
            {
                Id = id
            }).FirstOrDefault();
        }

        private string GetCustomsBrokerageName(int id)
        {
            var sql = @"select Name from [jetf].[dbo].[CustomsBrokerage]
                    where Id=@Id";

            return conn.Query<string>(sql, new
            {
                Id = id
            }).FirstOrDefault();
        }


        //取得編輯歷史記錄
        public List<SeaClearanceDetailEditHistoryModel> GetEditHistory(int seaClearanceDetailId)
        {
            var sql = @"
                SELECT [FieldName], [OldValue], [NewValue],[Memo], [EditTime], [EditUser]
                FROM jetf.dbo.SeaClearanceDetailEditHistory 
                WHERE SeaClearanceDetailId = @SeaClearanceDetailId
                ORDER BY EditTime DESC
            ";

            return conn.Query<SeaClearanceDetailEditHistoryModel>(sql, new
            {
                SeaClearanceDetailId = seaClearanceDetailId
            }).ToList();
        }

        /// <summary>
        /// 記錄簽審類別編輯歷史
        /// </summary>
        /// <param name="seaClearanceDetailId"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <param name="userId"></param>
        public void RecordApprovalCategoryEdit(int seaClearanceDetailId, string oldValue, string newValue, string userId)
        {
            //如果新舊值相同，則不記錄
            if (oldValue == newValue)
                return;

            var sql = @"
                INSERT INTO jetf.dbo.SeaClearanceDetailEditHistory 
                (SeaClearanceDetailId, FieldName, OldValue, NewValue, EditTime, EditUser)
                VALUES 
                (@SeaClearanceDetailId, @FieldName, @OldValue, @NewValue, @EditTime, @EditUser)
            ";

            conn.Execute(sql, new
            {
                SeaClearanceDetailId = seaClearanceDetailId,
                FieldName = "簽審類別",
                OldValue = oldValue,
                NewValue = newValue,
                EditTime = DateTime.Now,
                EditUser = userId
            });
        }
    }
}