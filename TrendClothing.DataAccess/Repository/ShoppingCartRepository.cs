using EcommProject_112.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository;
using TrendClothing.Models;

namespace EcommProject_112.DataAccess.Repository
{
    public class ShoppingCartRepository:Repository<ShoppingCart>, IshoppingCartRepository
    {
        private readonly ApplicationDbContext _context;
        public ShoppingCartRepository(ApplicationDbContext context):base(context)
        {
            _context = context;
        }
    }
}
