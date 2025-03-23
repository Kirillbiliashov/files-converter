using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using backend.BL.Converter;
using backend.Models.Db;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/convert")]
    public class ConvertApiController : ControllerBase
    {

        private readonly Func<string, IFileConverter> _converterFactory;
        private readonly IMongoDatabase _db;
        private readonly AzureBlobService _azureBlobService;

        public string? UserId 
        {
            get 
            {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            return identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
        }

        public ConvertApiController(Func<string, IFileConverter> converterFactory, IMongoDatabase db, AzureBlobService azureBlobService)
        {
            _converterFactory = converterFactory;
            _db = db;
            _azureBlobService = azureBlobService;
        }

        [Authorize]
        [HttpPost("")]
        public async Task<IActionResult> ConvertFile(IFormFile file, [FromForm] string outputFormat)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            try
            {
                var conversionResult = await ConvertFileAsync(file, outputFormat.ToLower());
                return File(conversionResult.OutputBytes, conversionResult.MimeType, conversionResult.Filename);
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Conversion failed. Error: {e.Message}");
            }
        }

        [HttpPost("all")]
        public async Task<IActionResult> ConvertMultipleFiles(IFormFileCollection files)
        {
            var metadata = JsonSerializer.Deserialize<List<ConvertMetadata>>(Request.Form["metadata"],
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var tasks = new List<Task<ConversionResult>>();
            foreach (var file in files)
            {
                var fileMetadata = metadata?.FirstOrDefault(m => m.FileName == file.FileName);
                if (fileMetadata == null)
                {
                    continue;
                }
                tasks.Add(ConvertFileAsync(file, fileMetadata.OutputFormat.ToLower()));
            }

            var results = await Task.WhenAll(tasks);
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var result in results)
                    {
                        if (result == null)
                        {
                            continue;
                        }

                        var zipEntry = archive.CreateEntry(result.Filename, CompressionLevel.Fastest);
                        using (var entryStream = zipEntry.Open())
                        {
                            await entryStream.WriteAsync(result.OutputBytes, 0, result.OutputBytes.Length);
                        }
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);

                return File(memoryStream.ToArray(), "application/zip", "files.zip");
            }

        }

        private async Task<ConversionResult> ConvertFileAsync(IFormFile file, string outputFormat)
        {
            var sw = Stopwatch.StartNew();

            var inputFileExtension = Path.GetExtension(file.FileName);
            var tempPath = Path.GetTempPath();
            var tempId = Guid.NewGuid();
            var inputFilePath = Path.Combine(tempPath, file.FileName);
            var outputFilePath = inputFilePath.Replace(inputFileExtension, $".{outputFormat}");

            try
            {
                using (var stream = new FileStream(inputFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var converter = _converterFactory(outputFormat);
                var conversionResult = await converter.ConvertFile(inputFilePath, outputFilePath);

                sw.Stop();
                var timeElapsed = sw.ElapsedMilliseconds;

                var blobName = $"output/{UserId}/{Guid.NewGuid()}/{conversionResult.Filename}";
                var outputUrl = await _azureBlobService.UploadFileAsync(blobName, conversionResult.OutputBytes);

                var conversion = new Conversion
                {
                    UserId = ObjectId.Parse(UserId),
                    Date = DateTime.UtcNow,
                    InputFormat = Path.GetExtension(file.FileName).ToLower().Substring(1),
                    OutputFormat = outputFormat.ToLower(),
                    Filename = file.FileName,
                    FileSize = conversionResult.OutputBytes.Length,
                    Status = "success",
                    TimeMsecs = timeElapsed,
                    OutputUrl = outputUrl
                };
                await _db.GetCollection<Conversion>("conversions").InsertOneAsync(conversion);

                return conversionResult;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                throw;
                // return StatusCode(500, $"Conversion failed. Error: {e.Message}");
            }
            finally
            {
                if (Path.Exists(inputFilePath))
                {
                    System.IO.File.Delete(inputFilePath);
                }

                if (Path.Exists(outputFilePath))
                {
                    System.IO.File.Delete(outputFilePath);
                }
            }
        }


    }

    public class ConvertMetadata
    {
        public string FileName { get; set; }
        public string OutputFormat { get; set; }
    }

}
