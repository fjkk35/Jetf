using System;

namespace Service.Services.SearchCargo.Domain
{
/// <summary>
    /// 掃貨上車資料模型
 /// </summary>
internal class PdtScanCargoUploadModel
    {
 public string TransName { get; set; }
        public string CarNo { get; set; }
     public DateTime? UploadTime { get; set; }
   public string UploadOpe { get; set; }
    }
}
