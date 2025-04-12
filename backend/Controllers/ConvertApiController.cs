using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using backend.BL.Converter;
using backend.BL.Encryption;
using backend.Models.Db;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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
        private readonly IConversionRepository _conversionRepository;
        private readonly AzureBlobService _azureBlobService;
        private readonly IEncryptor _encryptor;
        private readonly IEncryptionKeyStorage _encryptionKeyStorage;

        public string? UserId
        {
            get
            {
                var identity = HttpContext.User.Identity as ClaimsIdentity;
                return identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
        }

        public ConvertApiController(
            Func<string, IFileConverter> converterFactory,
            IConversionRepository conversionRepository,
            AzureBlobService azureBlobService,
            IEncryptor encryptor,
            IEncryptionKeyStorage encryptionKeyStorage)
        {
            _converterFactory = converterFactory;
            _conversionRepository = conversionRepository;
            _azureBlobService = azureBlobService;
            _encryptor = encryptor;
            _encryptionKeyStorage = encryptionKeyStorage;
        }

        [HttpPost("")]
        public async Task<IActionResult> ConvertFile(IFormFile file, [FromForm] string outputFormat, [FromForm] string? filename)
        {
            try
            {
                var conversion = await ConvertFileAsync(file, outputFormat.ToLower(), filename);
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
                tasks[f.Second.Id] = ConvertFileAsync(f.First, f.Second.OutputFormat.ToLower(), f.Second.FileName);
            }

            await Task.WhenAll(tasks.Values);

            return Ok(tasks.Select(p => new
            {
                Id = p.Key,
                Conversion = p.Value.Result
            }));
        }

        private async Task<Conversion> ConvertFileAsync(IFormFile file, string outputFormat, string? filename)
        {
            var sw = Stopwatch.StartNew();

            var inputFileExtension = Path.GetExtension(file.FileName);
            var tempPath = Path.GetTempPath();
            var tempId = Guid.NewGuid();
            var inputFilePath = Path.Combine(tempPath, file.FileName);
            var outputFilePath = inputFilePath.Replace(inputFileExtension, $".{outputFormat}");
            if (!string.IsNullOrWhiteSpace(filename))
            {

                outputFilePath = outputFilePath.Replace(Path.GetFileNameWithoutExtension(file.FileName), filename);
            }

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

                var outputUrl = await GetConvertedFileOutputUrl(conversionResult.Filename, conversionResult.OutputBytes);
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

                await _conversionRepository.AddConversion(conversion);

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

                await _conversionRepository.AddConversion(conversion);

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


        private async Task<string> GetConvertedFileOutputUrl(string filename, byte[] outputBytes)
        {
            byte[] blobBytes = outputBytes;
            if (UserId != null)
            {
                var encryptionKey = await _encryptionKeyStorage.GetKey(UserId);
                blobBytes = _encryptor.EncryptData(outputBytes, encryptionKey);
            }

            var primaryFolder = UserId ?? "anon";
            var blobName = $"output/{primaryFolder}/{Guid.NewGuid()}/{filename}";
            return await _azureBlobService.UploadFileAsync(blobName, blobBytes);
        }

        [HttpPost("download/{conversionId}")]
        public async Task<IActionResult> DownloadConversion(string conversionId)
        {
            var conversion = await _conversionRepository.GetConversion(conversionId);
            if (conversion == null)
            {
                return NotFound();
            }

            var blobBytes = await _azureBlobService.DownloadFileAsync(conversion.OutputUrl);
            if (UserId != null)
            {
                var encryptionKey = await _encryptionKeyStorage.GetKey(UserId);
                blobBytes = _encryptor.DecryptData(blobBytes, encryptionKey);
            }

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
