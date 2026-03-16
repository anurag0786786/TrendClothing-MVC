using System.Collections.Generic;
using TrendClothing.Models;

namespace TrendClothing.Models.ViewModels
{
    public class ProfileVM
    {
        public int ProfileId { get; set; }

        // Identity
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // UserProfile
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; }

        // ✅ FIX: SavedAddresses was missing from original VM
        // Used in Profile/Index.cshtml to show all saved addresses
        public List<Address> SavedAddresses { get; set; } = new();
    }
}