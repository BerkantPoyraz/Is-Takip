using System.Threading.Tasks;
using WorkTracking.Model.Model;

namespace WorkTracking.DAL.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByUsernameAndPasswordAsync(string username, string password);
        Task<User> GetUserByCredentialsAsync(string username, string password);
        User GetLoggedInUser();
        Task UpdateAsync(User user);
        Task DeleteAsync(int userId);
    }
}
