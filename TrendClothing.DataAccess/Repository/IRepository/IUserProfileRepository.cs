using TrendClothing.Models;

namespace TrendClothing.DataAccess.Repository.IRepository
{
    public interface IUserProfileRepository : IRepository<UserProfile>
    {
        UserProfile GetByUserId(string userId);
    }
}
