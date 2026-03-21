using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class Gb330Model
    {
        public string ClearanceDate { get; set; }
        public string LastPorCd { get; set; }
        public string ExielmmNote { get; set; }
        public string VoyageFlightNo { get; set; }
        public string ExCustCd { get; set; }
        public object ActuArDate { get; set; }
        public object ActuDpDate { get; set; }
        public string TransTypeCd { get; set; }
        public string Status { get; set; }
        public string ImoNo { get; set; }
        public string Choice { get; set; }
        public string ShipName { get; set; }
        public string VslSign { get; set; }
        public List<Gb330GridModel> GridModel { get; set; }
        public string NextPorCd { get; set; }
        public string Msg { get; set; }
        public string VslRegNo { get; set; }
        public string ShipCoCd { get; set; }
        public object EstClearanceDate { get; set; }
        public string WharfCd { get; set; }
        public object EstDpDate { get; set; }
        public string EstArDate { get; set; }
        public object VoyageNo { get; set; }
    }

    public class Gb330GridModel
    {
        public string ActuArDate { get; set; }
        public string ActuDpDate { get; set; }
        public string ClearanceDate { get; set; }
        public string EstArDate { get; set; }
        public object EstClearanceDate { get; set; }
        public object EstDpDate { get; set; }
        public string ExCustCd { get; set; }
        public string ExielmmNote { get; set; }
        public string ImoNo { get; set; }
        public string LastPorCd { get; set; }
        public string NextPorCd { get; set; }
        public object SeqNo { get; set; }
        public string ShipCoCd { get; set; }
        public string TransTypeCd { get; set; }
        public string VoyageFlightNo { get; set; }
        public object VoyageNo { get; set; }
        public string VslName { get; set; }
        public string VslRegNo { get; set; }
        public string VslSign { get; set; }
        public string WharfCd { get; set; }
    }
}
