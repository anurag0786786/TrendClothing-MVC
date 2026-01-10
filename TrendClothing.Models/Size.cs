using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrendClothing.Models
{
    public class Size
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
