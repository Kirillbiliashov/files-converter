using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;

namespace backend.Repositories
{
    public interface IFileInteractionsRepository
    {
        Task<List<FileInteraction>> GetFileInteractions(string userId);

        Task AddInteractions(IEnumerable<FileInteraction> interactions);

        Task DeleteUserInteractions(string userId);
    }
}