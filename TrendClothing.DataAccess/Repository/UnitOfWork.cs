using System.Threading.Tasks;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;

namespace TrendClothing.DataAccess.Repository
{
    public class UnitOfWork : IUnitofWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            category = new CategoryRepository(_context);
            productType = new ProductTypeRepository(_context);
            brand = new Repository<Brand>(_context);
            color = new Repository<Color>(_context);
            size = new Repository<Size>(_context);
            product = new Repository<Product>(_context);
            ProductVariant = new Repository<ProductVariant>(_context);
            ApplicationUser = new Repository<ApplicationUser>(_context);
            OrderHeader = new Repository<OrderHeader>(_context);
            OrderDetails = new Repository<OrderDetails>(_context);
            ShoppingCart = new Repository<ShoppingCart>(_context);
            Address = new AddressRepository(_context);
            UserProfile = new UserProfileRepository(_context);
            Wishlist = new Repository<Wishlist>(_context);
            ProductReview = new Repository<ProductReview>(_context);
            SiteImage = new Repository<SiteImage>(_context);
            HeroImage = new Repository<HeroImage>(_context);
        }

        public ICategoryRepository category { get; private set; }
        public IProductTypeRepository productType { get; private set; }
        public IRepository<Brand> brand { get; private set; }
        public IRepository<Color> color { get; private set; }
        public IRepository<Size> size { get; private set; }
        public IRepository<Product> product { get; private set; }
        public IRepository<ProductVariant> ProductVariant { get; private set; }
        public IRepository<ApplicationUser> ApplicationUser { get; private set; }
        public IRepository<OrderHeader> OrderHeader { get; private set; }
        public IRepository<OrderDetails> OrderDetails { get; private set; }
        public IRepository<ShoppingCart> ShoppingCart { get; private set; }
        public IRepository<Address> Address { get; private set; }
        public IRepository<UserProfile> UserProfile { get; private set; }
        public IRepository<Wishlist> Wishlist { get; private set; }
        public IRepository<ProductReview> ProductReview { get; private set; }
        public IRepository<SiteImage> SiteImage { get; private set; }
        public IRepository<HeroImage> HeroImage { get; private set; }

        public void Save() => _context.SaveChanges();
    }
}