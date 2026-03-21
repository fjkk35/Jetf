using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.TpctContainer
{
    public class TpctContainerModel
    {
        public string ContainerNo { get; set; }

        public string Date { get; set; }

        public string Time { get; set; }

        public string ContainerMovesDescription { get; set; }

        public string VesselVoyage { get; set; }

        public string Company { get; set; }

        public string Msg { get; set; }
    }
}
