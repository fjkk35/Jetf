using Renci.SshNet.Messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models
{
    /// <summary>
    /// 執行狀態參數
    /// </summary>
    public class Status
    {
        public static string success = "success";
        public static string error = "error";
    }

    public class ResopnseModel
    {
        public ResopnseModel() 
        {
            IsSuccess = true;
            status = Status.success;
        }

        public ResopnseModel(string message)
        {
            status = Status.error;
            msg = message;
        }

        public ResopnseModel(object returnObject)
        {
            IsSuccess = true;
            status = Status.success;
            ReturnObject = returnObject;
        }

        public bool IsSuccess { get; set; }

        public string status { get; set; }

        public string msg { get; set; } = "";

        public object ReturnObject { get; set; }
    }


    public class DataTableModel
    {
        public string status { get; set; }
        public string msg { get; set; } = "";

        public DataTable dt { get; set; }
    }

    public class JDataTableModel
    {
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }
        public string data { get; set; }
    }


}
