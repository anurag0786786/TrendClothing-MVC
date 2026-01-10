using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Models
{
    public class ProductType
    {
        public int Id { get; set; }
        
        public string Name { get; set; }
        public int CategoryId { get; set; }
        [ValidateNever]
        public Category category { get; set; }
        
    }

}
