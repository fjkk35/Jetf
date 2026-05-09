using System.Collections.Generic;

namespace Service.Models.SeaClearanceCreate
{
    public class SeaClearanceCreateResponse
    {
        public List<SeaClearanceModel> Data { get; set; } = new List<SeaClearanceModel>();

        public int TotalCount { get; set; }
    }
}