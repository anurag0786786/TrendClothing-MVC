using EcommProject_112.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository;
using TrendClothing.Models;

namespace EcommProject_112.DataAccess.Repository
{
    public class OrderDetailsRepository:Repository<OrderDetails>, IOrderDetailRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderDetailsRepository(ApplicationDbContext context):base(context)
        {
            _context = context;
        }

    }
}
