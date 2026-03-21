using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.ErrorOrderSendCustomer
{
    public class ErrorOrderSendCustomerViewModel
    {
        public ErrorOrderSendCustomerViewModel()
        {
            List = new List<ErrorOrderSendCustomer>();
        }

        public List<ErrorOrderSendCustomer> List { get; set; }
    }

    public class ErrorOrderSendCustomer
    {
        public int Id { get; set; }

        public string Customer { get; set; }

        public string Platform { get; set; }
    }




}