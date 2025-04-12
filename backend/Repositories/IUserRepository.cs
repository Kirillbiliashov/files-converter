using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;
using backend.Models.DTO;

namespace backend.Repositories
{
    public interface IUserRepository
    {
        Task<long> GetUsersCount(string email);

        Task AddUser(User user);

        Task<User> GetUserByEmail(string email);

        Task UpdateLastLoginTime(string userId);

        Task<UserInfo> GetUserInfo(string userId);

        Task DeleteUser(string userId);

        Task UpdateUserPreferences(string userId, bool deleteFilesAutomatically);

        Task<List<Conversion>> GetConversionsForDeletion();
    }
}