using System;

namespace Service.Services.SearchCargo.Domain
{
    /// <summary>
    /// 處置說明資訊Model
    /// </summary>
    public class ProcessInfoModel
    {
        public string ID { get; set; }
        public string PROCESS_TYPE_RAW { get; set; }
        public string PROCESS_TYPE { get; set; }
        public string REMARK { get; set; }
        public string FILENAME { get; set; }
        public string FILEPATH { get; set; }
        public string USER_NAME { get; set; }
        public DateTime? CRTDATETIME { get; set; }
        public string FormatCrtDateTime { get; set; }
        public string FINISH { get; set; }
        public string FINISH_USER_NAME { get; set; }
    }
}
