using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendClothing.Models
{
    public class Product : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public double Price { get; set; }

        // ✅ FIX: Added validation — DiscountPrice cannot be >= Price
        [Range(0, double.MaxValue, ErrorMessage = "Discount price cannot be negative")]
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

        public bool IsActive { get; set; } = true;

        // ✅ NEW: IValidatableObject — custom cross-field validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DiscountPrice.HasValue && DiscountPrice >= Price)
            {
                yield return new ValidationResult(
                    "Discount price must be less than the original price.",
                    new[] { nameof(DiscountPrice) }
                );
            }
        }
    }
}