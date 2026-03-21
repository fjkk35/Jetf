using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.UserMaster
{
    public class ResetPasswordModel
    {
        public string Id { get; set; }

        public string Password { get; set; }

        public string CheckPassword { get; set; }
    }
}
