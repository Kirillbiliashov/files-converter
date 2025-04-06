using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Services.Background
{
    public class DeleteFilesBackgroundService : BackgroundService
    {
        private Timer _timer;

        private readonly AzureBlobService _azureBlobService;
        private readonly IServiceProvider _serviceProvider;

        public DeleteFilesBackgroundService(AzureBlobService azureBlobService, IServiceProvider serviceProvider)
        {
            _azureBlobService = azureBlobService;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                        await DeleteFilesAsync(db);
                    }

                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception message: {ex.Message}");
                }
            }
        }

        protected async Task DeleteFilesAsync(IMongoDatabase db)
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

            var conversions = await db.GetCollection<User>("users")
            .Aggregate()
            .AppendStage<BsonDocument>(matchUsers)
            .AppendStage<BsonDocument>(lookupConversions)
            .AppendStage<BsonDocument>(unwindConversions)
            .AppendStage<BsonDocument>(matchDate)
            .AppendStage<Conversion>(replaceRoot)
            .ToListAsync();

            var conversionsChunks = conversions.Chunk(10);
            foreach (var chunk in conversionsChunks)
            {
                var deleteTasks = chunk.Select(c => _azureBlobService.DeleteFileAsync(c.OutputUrl));
                await Task.WhenAll(deleteTasks);
            }

            var conversionObejctIds = conversions.Select(c => c.Id);
            var filter = Builders<Conversion>.Filter.In("_id", conversionObejctIds);
            var update = Builders<Conversion>.Update.Unset("outputUrl");

            await db.GetCollection<Conversion>("conversions").UpdateManyAsync(filter, update);
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {

            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}