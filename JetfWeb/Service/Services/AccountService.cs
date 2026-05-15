using Service.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
   public class AccountService : _BaseService
   {
      public AccountService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
          : base(jetfDbContext, dataCenterDbContext)
      {
      }

        public UserMasterModel GetUserMaster(string user,string pwd) 
        {
            UserMasterModel model = new UserMasterModel();
            try
            {
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter("select * from [jetf].[dbo].[USER_MASTER] where [USER_ID]=@USER_ID and USER_PASSWORD=@USER_PASSWORD and USER_STATUS='1'", conn))
                {
                    da.SelectCommand.Parameters.Add("@USER_ID", SqlDbType.NVarChar).Value = user;
                    da.SelectCommand.Parameters.Add("@USER_PASSWORD", SqlDbType.NVarChar).Value = AesEncrypt(pwd);
                    da.Fill(dt);
                }
                if (dt.Rows.Count > 0)
                {
                    model.Status = Status.success;
                    model.Id = dt.Rows[0]["user_id"].ToString().Trim();
                    model.Name = dt.Rows[0]["user_name"].ToString().Trim();
                    model.Msg = "登入成功";
                }
                else
                {
                    model.Status = Status.error;
                    model.Msg = "帳號或密碼錯誤";
                }
            }
            catch (Exception ex)
            {
                model.Status = Status.error;
                model.Msg = ex.Message;
            }
           

            return model;
        }

        /// <summary>
        /// 取得權限
        /// </summary>
        /// <returns></returns>
        public Tuple<List<string>, List<string>> GetAuthority(string user) 
        {
            string sql = @"
                            SELECT DISTINCT c.AuthorityId, d.PartnerId 
                            FROM [jetf].[dbo].[USER_MASTER] a
                            JOIN [dbo].[UserAuthorityGroup] uag ON a.[USER_ID] = uag.[UserId]
                            JOIN [dbo].[AuthorityGroup] b ON uag.[AuthorityGroupId] = b.[Id]
                            JOIN [dbo].[AuthorityGroupDetail] c ON b.Id = c.AuthorityGroupId
                            JOIN [jetf].[dbo].[Authority] d ON c.AuthorityId = d.Id
                            WHERE a.[USER_ID] = @USER_ID
                            ";
            DataTable dt = new DataTable();
            using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
            {
                da.SelectCommand.Parameters.Add("@USER_ID", SqlDbType.NVarChar).Value = user;
                da.Fill(dt);
            }
            var authorities = dt.AsEnumerable().Select(r => r.Field<string>("AuthorityId")).ToList();
            var partners = dt.AsEnumerable().Select(r => r.Field<string>("PartnerId")).Distinct().ToList();
            return Tuple.Create(partners, authorities);
        }
    }
}
