using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Db;
using backend.Repositories;
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
                        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                        var conversionRepository = scope.ServiceProvider.GetRequiredService<IConversionRepository>();
                        await DeleteFilesAsync(userRepository, conversionRepository);
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

        protected async Task DeleteFilesAsync(IUserRepository userRepository, IConversionRepository conversionRepository)
        {
            var conversions = await userRepository.GetConversionsForDeletion();
            var conversionsChunks = conversions.Chunk(10);
            foreach (var chunk in conversionsChunks)
            {
                var deleteTasks = chunk.Select(c => _azureBlobService.DeleteFileAsync(c.OutputUrl));
                await Task.WhenAll(deleteTasks);
            }

            var conversionObejctIds = conversions.Select(c => c.Id);
            await conversionRepository.RemoveOutputUlrs(conversionObejctIds);
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