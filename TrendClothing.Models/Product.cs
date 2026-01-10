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
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public double  Price { get; set; }
         
        public double? DiscountPrice { get; set; }


      
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        [ValidateNever]
        
        public Category Category { get; set; }


       
        [Display(Name = "Product Type")]
        public int ProductTypeId { get; set; }
        [ValidateNever]
        public ProductType ProductType { get; set; }


        [Display(Name = "Brand")]
        public int BrandId { get; set; }
        [ValidateNever]
        public Brand Brand { get; set; }

        
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }=true;



    }
}
