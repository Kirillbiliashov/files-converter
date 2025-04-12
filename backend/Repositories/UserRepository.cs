using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;
using backend.Models.DTO;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Repositories
{
    public class UserRepository : IUserRepository
    {

        private readonly IMongoCollection<User> _users;

        public UserRepository(IMongoDatabase db)
        {
            _users = db.GetCollection<User>("users");
        }

        public async Task AddUser(User user)
        {
            await _users.InsertOneAsync(user);
        }

        public async Task DeleteUser(string userId)
        {
            await _users.DeleteOneAsync(u => u.Id == ObjectId.Parse(userId));
        }

        public async Task<List<Conversion>> GetConversionsForDeletion()
        {
            var matchUsers = new BsonDocument("$match", new BsonDocument("deleteFiles", true));

            var lookupConversions = new BsonDocument("$lookup",
                new BsonDocument
                {
                    { "from", "conversions" },
                    { "localField", "_id" },
                    { "foreignField", "userId" },
                    { "as", "conversions" }
                });

            var unwindConversions = new BsonDocument("$unwind", "$conversions");

            var matchDate = new BsonDocument("$match", new BsonDocument
            {
                { "conversions.date", new BsonDocument("$lte", BsonDateTime.Create(DateTime.UtcNow.AddDays(-30))) },
                { "conversions.outputUrl", new BsonDocument("$exists", true) },
            });

            var replaceRoot = new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$conversions"));

            return await _users
            .Aggregate()
            .AppendStage<BsonDocument>(matchUsers)
            .AppendStage<BsonDocument>(lookupConversions)
            .AppendStage<BsonDocument>(unwindConversions)
            .AppendStage<BsonDocument>(matchDate)
            .AppendStage<Conversion>(replaceRoot)
            .ToListAsync();
        }

        public async Task<User> GetUserByEmail(string email)
        {
            return await _users
            .Find(u => u.Email == email)
            .SingleOrDefaultAsync();
        }

        public async Task<UserInfo> GetUserInfo(string userId)
        {
            var match = new BsonDocument("$match", new BsonDocument("_id", ObjectId.Parse(userId)));
            var lookup = new BsonDocument("$lookup",
                new BsonDocument
                {
                    { "from", "accessTokens" },
                    { "localField", "_id" },
                    { "foreignField", "userId" },
                    { "as", "providers" }
                });
            var project = new BsonDocument("$project",
                new BsonDocument
                {
                    { "providers._id", 0 },
                    { "_id", 0 },
                    { "providers.refreshToken", 0 },
                });

            var pipeline = new[] { match, lookup, project };
            return await _users.Aggregate<UserInfo>(pipeline).FirstOrDefaultAsync();
        }

        public async Task<long> GetUsersCount(string email) =>
        await _users.CountDocumentsAsync(u => u.Email == email);

        public async Task UpdateLastLoginTime(string userId)
        {
            var filter = Builders<User>.Filter.Eq(doc => doc.Id, ObjectId.Parse(userId));
            var update = Builders<User>.Update.Set(doc => doc.LastLogin, DateTime.UtcNow);

            await _users.UpdateOneAsync(filter, update);
        }

        public async Task UpdateUserPreferences(string userId, bool deleteFilesAutomatically)
        {
            var filter = Builders<User>.Filter.Eq("_id", ObjectId.Parse(userId));
            var update = Builders<User>.Update.Combine(
                Builders<User>.Update.Set("deleteFiles", deleteFilesAutomatically)
            );

            await _users.UpdateOneAsync(filter, update);
        }
    }
}