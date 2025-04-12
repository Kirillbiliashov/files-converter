using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;
using MongoDB.Bson;

namespace backend.Repositories
{
    public interface IConversionRepository
    {
        Task AddConversion(Conversion conversion);

        Task<List<Conversion>> GetConversions(string userId);

        Task<Conversion> GetConversion(string id);

        Task<List<string>> GetConversionUrls(string userId);

        Task DeleteConversions(string userId);

        Task RemoveOutputUlrs(IEnumerable<ObjectId> conversionIds);
    }
}