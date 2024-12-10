using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkTracking.Model.Model;
using WorkTracking.DAL.Data;

namespace WorkTracking.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByUsernameAndPasswordAsync(string username, string password)
        {
            return await _context.Users
                .Where(u => u.UserName == username && u.Password == password)
                .FirstOrDefaultAsync();
        }
        public async Task<User> GetUserByCredentialsAsync(string username, string password)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username && u.Password == password);
        }

        public User GetLoggedInUser()
        {
            var userId = 1;

            return _context.Users.FirstOrDefault(u => u.UserId == userId);
        }
        public async Task UpdateAsync(User user)
        {
            var existingUser = await _context.Users.FindAsync(user.UserId);
            if (existingUser != null)
            {
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.UserName = user.UserName;
                existingUser.Password = user.Password;
                existingUser.Salary = user.Salary;
                existingUser.HireDate = user.HireDate;
                existingUser.Role = user.Role;

                await _context.SaveChangesAsync();
            }
        }
        public User GetLoggedInUser(int userId)
        {
            return _context.Users.FirstOrDefault(u => u.UserId == userId);
        }
        public async Task DeleteAsync(int userId)
        {
            var userToDelete = await _context.Users.FindAsync(userId);

            if (userToDelete != null)
            {
                _context.Users.Remove(userToDelete);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Kullanıcı bulunamadı!");
            }
        }

    }
}
