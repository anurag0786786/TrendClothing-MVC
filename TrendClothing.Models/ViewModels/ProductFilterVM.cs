using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Models.ViewModels
{
    public class ProductFilterVM
    {
        public string? Search { get; set; }
        public string? Sort { get; set; }

        public List<int> BrandIds { get; set; } = new();
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public int? MinDiscount { get; set; }

        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<Brand> Brands { get; set; }

    }


}
