using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Repositories
{
    public class FileInteractionsRepository : IFileInteractionsRepository
    {
        private readonly IMongoCollection<FileInteraction> _fileInteractions;
        public FileInteractionsRepository(IMongoDatabase db)
        {
            _fileInteractions = db.GetCollection<FileInteraction>("fileInteractions");
        }

        public async Task AddInteractions(IEnumerable<FileInteraction> interactions)
        {
            await _fileInteractions.InsertManyAsync(interactions);
        }

        public async Task DeleteUserInteractions(string userId)
        {
            await _fileInteractions.DeleteManyAsync(i => i.UserId == ObjectId.Parse(userId));
        }

        public async Task<List<FileInteraction>> GetFileInteractions(string userId)
        {
            return await _fileInteractions
            .Find(c => c.UserId == ObjectId.Parse(userId))
            .ToListAsync();
        }
    }
}