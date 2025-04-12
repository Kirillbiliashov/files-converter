using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Repositories
{
    public class ConversionRepository : IConversionRepository
    {
        private readonly IMongoCollection<Conversion> _conversions;

        public ConversionRepository(IMongoDatabase db)
        {
            _conversions = db.GetCollection<Conversion>("conversions");
        }

        public async Task AddConversion(Conversion conversion)
        {
            await _conversions.InsertOneAsync(conversion);
        }

        public async Task DeleteConversions(string userId)
        {
            await _conversions.DeleteManyAsync(i => i.UserId == ObjectId.Parse(userId));
        }

        public async Task<Conversion> GetConversion(string id)
        {
            return await _conversions
            .FindSync(c => c.Id == ObjectId.Parse(id))
            .SingleOrDefaultAsync();
        }

        public async Task<List<Conversion>> GetConversions(string userId)
        {
            return await _conversions
            .Find(c => c.UserId == ObjectId.Parse(userId))
            .SortByDescending(c => c.Date)
            .ToListAsync();
        }

        public async Task<List<string>> GetConversionUrls(string userId)
        {
            return await _conversions
            .Find(c => c.UserId == ObjectId.Parse(userId) && c.OutputUrl != null)
            .Project(c => c.OutputUrl)
            .ToListAsync();
        }

        public async Task RemoveOutputUlrs(IEnumerable<ObjectId> conversionIds)
        {
            var filter = Builders<Conversion>.Filter.In("_id", conversionIds);
            var update = Builders<Conversion>.Update.Unset("outputUrl");

            await _conversions.UpdateManyAsync(filter, update);
        }
    }
}