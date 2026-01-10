using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Models.ViewModels
{
    public class ProductDetailsVM
    {
        public Product Product { get; set; }
        public List<ProductVariant> Variants { get; set; }

        public int SelectedVariantId { get; set; }
        public int Count { get; set; } = 1;

        // PRICE RELATED
        public double SellingPrice { get; set; }   // discounted / final
        public double OriginalPrice { get; set; }  // MRP
        public int DiscountPercent { get; set; }   // % OFF
    }

}
