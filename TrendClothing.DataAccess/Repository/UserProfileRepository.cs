using System.Linq;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;

namespace TrendClothing.DataAccess.Repository
{
    public class UserProfileRepository
        : Repository<UserProfile>, IUserProfileRepository
    {
        private readonly ApplicationDbContext _db;

        public UserProfileRepository(ApplicationDbContext db)
            : base(db)
        {
            _db = db;
        }

        public UserProfile GetByUserId(string userId)
        {
            return _db.UserProfiles.FirstOrDefault(x => x.UserId == userId);
        }
    }
}
