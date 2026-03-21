using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CustomerTaxSetting
{
    /// <summary>
    /// SEA客戶模型
    /// </summary>
    public class SeaCustomerModel
    {
        /// <summary>
        /// 客戶代號
        /// </summary>
        public string Cust_Code { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string Cust_Name { get; set; }
    }

    /// <summary>
    /// 稅金時間模型
    /// </summary>
    public class TaxTimeModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 稅金時間
        /// </summary>
        public string TaxTime { get; set; }
    }

    /// <summary>
    /// 客戶稅金時間設定模型
    /// </summary>
    public class CustomerTaxSettingModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 客戶代號
        /// </summary>
        public string Cust_Code { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string Cust_Name { get; set; }

        /// <summary>
        /// 稅金時間ID列表
        /// </summary>
        public List<int> TaxTimeIds { get; set; }

        /// <summary>
        /// 稅金時間字串列表
        /// </summary>
        public List<string> TaxTimes { get; set; }

        public CustomerTaxSettingModel()
        {
            TaxTimeIds = new List<int>();
            TaxTimes = new List<string>();
        }
    }

    /// <summary>
    /// 客戶稅金時間設定顯示模型
    /// </summary>
    public class CustomerTaxSettingDisplayModel
    {
        /// <summary>
        /// 客戶代號
        /// </summary>
        public string Cust_Code { get; set; }

        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string Cust_Name { get; set; }

        /// <summary>
        /// 稅金時間設定 (Key: 稅金時間, Value: 是否勾選)
        /// </summary>
        public Dictionary<string, bool> TaxTimeSettings { get; set; }

        public CustomerTaxSettingDisplayModel()
        {
            TaxTimeSettings = new Dictionary<string, bool>();
        }
    }
}