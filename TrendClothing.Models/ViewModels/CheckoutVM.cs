using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Models.ViewModels
{
    public class CheckoutVM
    {
        public IEnumerable<Address> Addresses { get; set; }
        public int? SelectedAddressId { get; set; }

        public ShoppingCartVM Cart { get; set; }
    }

}
