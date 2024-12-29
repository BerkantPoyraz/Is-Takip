using System.Threading.Tasks;
using WorkTracking.Model.Model;

namespace WorkTracking.DAL.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByCredentialsAsync(string username, string password);
        Task<User> GetUserByUserNameAsync(string username);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int userId);
        User GetLoggedInUser();
        User GetLoggedInUser(int userId);
    }
}
