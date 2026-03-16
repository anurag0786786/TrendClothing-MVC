// ✅ FIX: Class was "OrederHeaderRepository" (typo) — renamed to OrderHeaderRepository
// ✅ FIX: Namespace was "EcommProject_112.DataAccess.Repository" (old project!) — fixed

using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;

namespace TrendClothing.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderHeaderRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // Custom methods can be added here
        // e.g. async get by userId, get with details included, etc.
    }
}