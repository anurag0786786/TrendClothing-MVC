using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            brand= new Repository<Brand>(_context);
            color= new Repository<Color>(_context);
            size= new Repository<Size>(_context);
            product= new Repository<Product>(_context);
            ProductVariant= new Repository<ProductVariant>(_context);
            ApplicationUser= new Repository<ApplicationUser>(_context);
            OrderHeader= new Repository<OrderHeader>(_context);
            OrderDetails= new Repository<OrderDetails>(_context);
            ShoppingCart= new Repository<ShoppingCart>(_context);
            Address= new AddressRepository(_context);
            UserProfile= new UserProfileRepository(_context);

        }
        public ICategoryRepository category {private set; get; } 


        public IProductTypeRepository productType { private set; get; }

        public IRepository<Brand> brand { private set; get; } 

        public IRepository<Color> color { private set; get; }

        public IRepository<Size> size { private set; get; }

        public IRepository<Product> product { private set; get; }
        public IRepository<ProductVariant> ProductVariant { private set; get; } 
        public IRepository<ApplicationUser> ApplicationUser { private set; get; }

        public IRepository<OrderHeader> OrderHeader { private set; get; }

        

        public IRepository<ShoppingCart> ShoppingCart { private set; get; }

        public IRepository<OrderDetails> OrderDetails { private set; get; }

        public IRepository<Address> Address { private set; get; }

        public IRepository<UserProfile> UserProfile { private set; get; }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
