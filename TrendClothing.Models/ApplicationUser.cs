using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Models
{
    public class ApplicationUser: IdentityUser
    {

        [Required]
        public string Name { get; set; }
        [Display(Name = "Street Address")]
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        [Display(Name = "Postal Code")]
        public String PostalCode { get; set; }
        [NotMapped]
        public string Roles { get; set; }

    }
    
} 


