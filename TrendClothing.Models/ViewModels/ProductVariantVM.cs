using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Models.ViewModels
{
    public class ProductVariantVM
    {
        public int ProductId { get; set; }

        // Multi Select
        public List<int> SelectedSizeIds { get; set; }
        public List<int> SelectedColorIds { get; set; }

        // Common values for all variants
        public double Price { get; set; }
        public int Stock { get; set; }
        public int SelectedVariantId { get; set; }

        public int Count { get; set; }

        // Dropdown Lists
        public IEnumerable<SelectListItem> ProductList { get; set; }
        public IEnumerable<SelectListItem> SizeList { get; set; }
        public IEnumerable<SelectListItem> ColorList { get; set; }
    }
}
