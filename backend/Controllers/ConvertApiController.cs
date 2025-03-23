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
                var conversionId = await ConvertFileAsync(file, outputFormat.ToLower());
                return Ok(new { ConversionId = conversionId });
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

            var tasks = new List<Task<string>>();
            foreach (var file in files)
            {
                var fileMetadata = metadata?.FirstOrDefault(m => m.FileName == file.FileName);
                if (fileMetadata == null)
                {
                    continue;
                }
                tasks.Add(ConvertFileAsync(file, fileMetadata.OutputFormat.ToLower()));
            }

            var conversionIds = await Task.WhenAll(tasks);
            return Ok(files.Select(f => f.FileName)
            .Zip(conversionIds)
            .Select(t => new
            {
                Filename = t.First,
                ConversionId = t.Second
            })
                );
        }

        private async Task<string> ConvertFileAsync(IFormFile file, string outputFormat)
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

                return conversion.IdInternal;
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


        [Authorize]
        [HttpPost("download/{conversionId}")]
        public async Task<IActionResult> DownloadConversion(string conversionId)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var conversion = await _db.GetCollection<Conversion>("conversions")
            .FindSync(c => c.Id == ObjectId.Parse(conversionId) && c.UserId == ObjectId.Parse(userId))
            .SingleOrDefaultAsync();
            if (conversion == null)
            {
                return NotFound();
            }

            var blobBytes = await _azureBlobService.DownloadFileAsync(conversion.OutputUrl);

            return File(blobBytes, "application/octet-stream", Path.GetFileName(conversion.OutputUrl));
        }


    }

    public class ConvertMetadata
    {
        public string FileName { get; set; }
        public string OutputFormat { get; set; }
    }

}
