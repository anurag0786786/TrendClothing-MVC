using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


      

namespace TrendClothing.Models
{
        public class ProductVariant
        {
            public int Id { get; set; }

            
            public int ProductId { get; set; }
            [ForeignKey("ProductId")]
            [ValidateNever]
            public Product Product { get; set; }

            public int SizeId { get; set; }
            [ForeignKey("SizeId")]
        [ValidateNever]
            public Size Size { get; set; }

           
            public int ColorId { get; set; }
            [ForeignKey("ColorId")]
        [ValidateNever]
            public Color Color { get; set; }

            public double Price { get; set; }
            public int Stock { get; set; }
        }
}


