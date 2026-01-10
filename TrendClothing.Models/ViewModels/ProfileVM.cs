using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Models.ViewModels
{
    public class ProfileVM
    {
        public int ProfileId { get; set; }

        // Identity
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        // UserProfile
        public string FullName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
    }


}
