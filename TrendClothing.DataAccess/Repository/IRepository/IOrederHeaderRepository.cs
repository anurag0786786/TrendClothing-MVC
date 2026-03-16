// ✅ FIX: File was "IOrederHeaderRepository.cs" (typo: "Oreder")
// Rename file to: IOrderHeaderRepository.cs
//
// ✅ FIX: Namespace was "EcommProject_112.DataAccess.Repository.IRepository"
// (old project name leaking) — fixed to TrendClothing namespace

using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;

namespace TrendClothing.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        // Add custom order-specific methods here if needed in future
        // e.g.: Task<IEnumerable<OrderHeader>> GetByUserIdAsync(string userId);
    }
}