using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Service.Services.UserMaster.Domain
{
    /// <summary>
    /// 會員清單傳輸物件
    /// </summary>
    public class UserMasterDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserStatus { get; set; }
        
        // 向下相容：保留單一權限群組欄位
        [Obsolete("此屬性已棄用，請使用 AuthorityGroups")]
        public int? AuthorityGroupId { get; set; }
        [Obsolete("此屬性已棄用，請使用 AuthorityGroups")]
        public string AuthorityGroupName { get; set; }
        
        // 新增：支援多個權限群組
        public List<AuthorityGroupDto> AuthorityGroups { get; set; } = new List<AuthorityGroupDto>();
        
        public string UpdOpe { get; set; }
        public DateTime? UpdTime { get; set; }

        /// <summary>
        /// 狀態顯示文字
        /// </summary>
        public string UserStatusText => UserStatus == "1" ? "啟用" : "停用";
    }

    /// <summary>
    /// 會員編輯資料傳輸物件
    /// </summary>
    public class UserMasterEditDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserStatus { get; set; }
        
        // 向下相容：保留單一權限群組欄位
        [Obsolete("此屬性已棄用，請使用 AuthorityGroupIds")]
        public int? AuthorityGroupId { get; set; }
        
        // 新增：支援多個權限群組ID
        public List<int> AuthorityGroupIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// 權限群組選項
    /// </summary>
    public class AuthorityGroupOptionDto
    {
        public int Id { get; set; }
        public string GroupName { get; set; }
    }

    /// <summary>
    /// 權限群組資料傳輸物件
    /// </summary>
    public class AuthorityGroupDto
    {
        public int GroupId { get; set; }  // 修改為 GroupId 以符合 SQL 查詢的欄位名稱
        public string GroupName { get; set; }
    }
}