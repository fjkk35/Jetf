using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearanceCustomer
{
    /// <summary>
    /// е琿め家
    /// </summary>
    public class SeaClearanceCustomerModel
    {
        public string Cust_Code { get; set; }
        public string Cust_Name { get; set; }
    }

    /// <summary>
    /// ノめ家ㄓ方 DATA_CENTER
    /// </summary>
    public class AvailableCustomerModel
    {
        public string Cust_Code { get; set; }
        public string Cust_Name { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// у秖巨叫―家
    /// </summary>
    public class CustomerBatchOperationModel
    {
        public List<string> CustomerCodes { get; set; }
        public string Operation { get; set; } // "Add" ┪ "Delete"
    }
}