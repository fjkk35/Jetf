using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Logistics
{
    /// <summary>
    /// 下載圖片請求模型
    /// </summary>
    public class DownLoadImageRequest
    {
        /// <summary>
        /// 客戶訂單號
        /// </summary>
        public string CusOrder { get; set; }

        /// <summary>
        /// 圖片類型
        /// </summary>
        public string ImageType { get; set; }
    }

    /// <summary>
    /// 下載圖片回應模型
    /// </summary>
    public class DownLoadImageResponse
    {
        /// <summary>
        /// 結果代碼
        /// </summary>
        public string ResultCode { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 圖片資料列表
        /// </summary>
        public ImageData[] Rows { get; set; }
    }

    /// <summary>
    /// 圖片資料
    /// </summary>
    public class ImageData
    {
        /// <summary>
        /// Base64 編碼的圖片資料
        /// </summary>
        public string Base64 { get; set; }

        /// <summary>
        /// 拍照時間
        /// </summary>
        public string PhotoTime { get; set; }
    }
}