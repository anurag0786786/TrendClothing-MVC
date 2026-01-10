using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Models.ViewModels
{
    public class OrderEmailVM
    {
        public int OrderId { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }

        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }

        public DateTime ExpectedFrom { get; set; }
        public DateTime ExpectedTo { get; set; }

        public List<(string ProductName, int Quantity)> Products { get; set; }
    }
}
