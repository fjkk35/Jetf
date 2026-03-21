using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class Gb302Model
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("transTypeCd")]
        public string TransTypeCd { get; set; }

        [JsonPropertyName("searchOper")]
        public string SearchOper { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("searchString")]
        public string SearchString { get; set; }

        // JSON 裡是 "class"，C# 關鍵字，改成 Clazz
        [JsonPropertyName("class")]
        public string Clazz { get; set; }

        [JsonPropertyName("gridModel")]
        public List<Gb302GridModel> GridModel { get; set; }

        [JsonPropertyName("searchField")]
        public string SearchField { get; set; }

        [JsonPropertyName("sidx")]
        public string Sidx { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }

        [JsonPropertyName("loadonce")]
        public bool Loadonce { get; set; }

        [JsonPropertyName("rows")]
        public int Rows { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("sord")]
        public string Sord { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("records")]
        public int Records { get; set; }
    }

    public class Gb302GridModel
    {
        [JsonPropertyName("dSeq")]
        public int DSeq { get; set; }

        [JsonPropertyName("itemNo")]
        public string ItemNo { get; set; }

        [JsonPropertyName("noticeDateTime")]
        public string NoticeDateTime { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; }

        /// <summary>
        /// 品名
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        /// 稅則
        /// </summary>
        public string CCCCode { get; set; }
    }
}
