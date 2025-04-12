using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;
using MongoDB.Bson;

namespace backend.Repositories
{
    public interface IAccessTokenRepository
    {
        Task<AccessToken> GetAccessToken(string userId, string provider);

        Task UpdateAccessToken(ObjectId id, string token, DateTime tokenExpiration);

        Task UpsertUserAccessToken(string userId, AccessToken token);

        Task DeleteAccessTokens(string userId);
    }
}