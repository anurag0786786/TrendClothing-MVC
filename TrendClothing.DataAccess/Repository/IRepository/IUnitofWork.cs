using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrendClothing.Models;

namespace TrendClothing.DataAccess.Repository.IRepository
{
    public interface IUnitofWork
    {
        ICategoryRepository category { get; }
        IProductTypeRepository productType { get; }
        IRepository<Brand> brand { get; }
        IRepository<Color> color { get; }
        IRepository<Size> size { get; }
        IRepository<Product> product { get; }
        IRepository<ProductVariant> ProductVariant { get; }
        IRepository<ApplicationUser> ApplicationUser { get; }
        IRepository<ShoppingCart> ShoppingCart { get; }
        IRepository<OrderHeader> OrderHeader { get; }
        IRepository<OrderDetails> OrderDetails { get; }
        IRepository<Address> Address { get; }
        IRepository<UserProfile> UserProfile { get; }




        void Save();
    }
}
