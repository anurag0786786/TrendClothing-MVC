namespace TrendClothing.Models
{
    public class SiteImage
    {
        public int Id { get; set; }

        // "Hero", "Men", "Women", "Children"
        public string Key { get; set; }

        public string ImageUrl { get; set; }
    }
}