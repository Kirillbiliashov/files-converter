using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Repositories
{
    public class AccessTokenRepository : IAccessTokenRepository
    {

        private readonly IMongoCollection<AccessToken> _accessTokens;

        public AccessTokenRepository(IMongoDatabase db)
        {
            _accessTokens = db.GetCollection<AccessToken>("accessTokens");
        }

        public async Task DeleteAccessTokens(string userId)
        {
            await _accessTokens.DeleteManyAsync(i => i.UserId == ObjectId.Parse(userId));
        }

        public async Task<AccessToken> GetAccessToken(string userId, string provider) =>
            await _accessTokens
            .Find(t => t.UserId == ObjectId.Parse(userId) && t.Provider == provider)
            .SingleOrDefaultAsync();

        public async Task UpdateAccessToken(ObjectId id, string token, DateTime tokenExpiration)
        {
            var filter = Builders<AccessToken>.Filter.Eq(t => t.Id, id);
            var update = Builders<AccessToken>.Update
                .Set(t => t.Token, token)
                .Set(t => t.TokenExpiration, tokenExpiration)
                .Set(t => t.ConnectedAt, DateTime.UtcNow);

            await _accessTokens.UpdateOneAsync(filter, update);
        }

        public async Task UpsertUserAccessToken(string userId, AccessToken token)
        {
            var filter = Builders<AccessToken>.Filter.Eq(t => t.UserId, ObjectId.Parse(userId));

            var update = Builders<AccessToken>.Update
                .Set(t => t.Token, token.Token)
                .Set(t => t.RefreshToken, token.RefreshToken)
                .Set(t => t.TokenExpiration, token.TokenExpiration)
                .Set(t => t.ConnectedAt, DateTime.UtcNow)
                .Set(t => t.Provider, token.Provider)
                .SetOnInsert(t => t.UserId, ObjectId.Parse(userId));

            var options = new UpdateOptions { IsUpsert = true };

            await _accessTokens.UpdateOneAsync(filter, update, options);
        }
    }
}