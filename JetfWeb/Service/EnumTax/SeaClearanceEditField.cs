using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum SeaClearanceEditField
    {
        [Description("報驗公司")]
        CustomsBrokerId = 1,

        [Description("簽審類別")]
        InspectionType = 2,

        [Description("入倉日期")]
        SignInTime = 3,

        [Description("出倉日期")]
        SignOutTime = 4,

        [Description("報關方式")]
        Post_Entry = 5,

        [Description("報單號碼")]
        DeclNo = 6,

        [Description("進口人統一編號")]
        Importer_Id = 7,

        [Description("原單申報人")]
        Importer = 8,

        [Description("聯繫人信箱")]
        ContactEmail = 9,

        [Description("聯繫人異動資料")]
        ContactChangeData = 10,

        [Description("收到正本選單")]
        ReceiveAuthorizationForm = 11,

        [Description("寄文件選單")]
        SendAuthorizationForm = 12,

        [Description("備註")]
        Remark = 13,

        [Description("步驟")]
        Step = 14,
        
        [Description("異常狀態")]
        AbnormalState = 15,

        [Description("扣倉")]
        IsCustomsHold = 16,

        [Description("扣倉項次")]
        CustomsHold = 17,

        [Description("代理報驗")]
        CustomsBrokerageId = 18,
    }


    public class TableNameAttribute : Attribute
    {
        public string TableName { get; }

        public TableNameAttribute(string tableName)
        {
            TableName = tableName;
        }
    }
}
