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

        [HttpPost("")]
        public async Task<IActionResult> ConvertFile(IFormFile file, [FromForm] string outputFormat)
        {
            try
            {
                var conversion = await ConvertFileAsync(file, outputFormat.ToLower());
                return Ok(new { Conversion = conversion });
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
            if (metadata == null)
            {
                return BadRequest();
            }

            var filesWithMetadata = files.Zip(metadata);
            var tasks = new Dictionary<string, Task<Conversion>>();
            foreach (var f in filesWithMetadata)
            {
                tasks[f.Second.Id] = ConvertFileAsync(f.First, f.Second.OutputFormat.ToLower());
            }

            await Task.WhenAll(tasks.Values);

            return Ok(tasks.Select(p => new 
            {
                Id = p.Key,
                Conversion = p.Value.Result
            }));
        }

        private async Task<Conversion> ConvertFileAsync(IFormFile file, string outputFormat)
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

                var primaryFolder = UserId ?? "anon";
                var blobName = $"output/{primaryFolder}/{Guid.NewGuid()}/{conversionResult.Filename}";
                var outputUrl = await _azureBlobService.UploadFileAsync(blobName, conversionResult.OutputBytes);
                Console.WriteLine($"Converted and written to {blobName}");
                var conversion = new Conversion
                {
                    UserId = UserId != null ? ObjectId.Parse(UserId) : null,
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

                return conversion;
            }
            catch (Exception e)
            {
                var conversion = new Conversion
                {
                    UserId = ObjectId.Parse(UserId),
                    Date = DateTime.UtcNow,
                    InputFormat = Path.GetExtension(file.FileName).ToLower().Substring(1),
                    OutputFormat = outputFormat.ToLower(),
                    Filename = file.FileName,
                    Status = "failed",
                };
                await _db.GetCollection<Conversion>("conversions").InsertOneAsync(conversion);

                Console.WriteLine($"Error: {e.Message}");
                return conversion;
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

        [HttpPost("download/{conversionId}")]
        public async Task<IActionResult> DownloadConversion(string conversionId)
        {
            var conversion = await _db.GetCollection<Conversion>("conversions")
            .FindSync(c => c.Id == ObjectId.Parse(conversionId))
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
        public string Id { get; set; }
    }

}
