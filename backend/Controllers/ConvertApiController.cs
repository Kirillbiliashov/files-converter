using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using backend.BL.Converter;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/convert")]
    public class ConvertApiController : ControllerBase
    {

        private static readonly Dictionary<string, string> _fileMimeTypeMap = new()
        {
            { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            { "doc",  "application/msword"},
            { "rtf",  "application/rtf"},
            { "txt", "text/plain"},
            { "odt", "application/vnd.oasis.opendocument.text"},
            { "pdf",   "application/pdf"},
            { "epub", "application/epub+zip" },
            { "html",  "text/html"},
            { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            { "xls",   "application/vnd.ms-excel" },
            { "ods",  "application/vnd.oasis.opendocument.spreadsheet"},
            { "csv",  "text/csv" },
            { "tsv",  "text/tab-separated-values" },
            { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
            { "ppt",   "application/vnd.ms-powerpoint"},
            { "odp",  "application/vnd.oasis.opendocument.presentation"},
            { "png", "image/png" },
            { "jpg", "image/jpeg" },
            { "jpeg", "image/jpeg" }
        };

        private readonly Func<string, IFileConverter> _converterFactory;

        public ConvertApiController(Func<string, IFileConverter> converterFactory)
        {
            _converterFactory = converterFactory;
        }

        [HttpPost("")]
        public async Task<IActionResult> ConvertFile(IFormFile file, [FromForm] string outputFormat)
        {
            var inputFileExtension = Path.GetExtension(file.FileName);
            var tempPath = Path.GetTempPath();
            var tempId = Guid.NewGuid();
            var inputFilePath = Path.Combine(tempPath, $"{tempId}{inputFileExtension}");
            var outputFilePath = inputFilePath.Replace(inputFileExtension, $".{outputFormat}");

            try
            {
                using (var stream = new FileStream(inputFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var converter = _converterFactory(outputFormat);
                var result = await converter.ConvertFile(inputFilePath, outputFilePath);

                return File(result.OutputBytes, result.MimeType, result.Filename);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Conversion failed. Error: {e.Message}");
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
}
