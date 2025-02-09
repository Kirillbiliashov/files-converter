using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/convert")]
    public class ConvertApiController : ControllerBase
    {

        private static readonly List<string> imageFormats = new() { "png", "jpg", "jpeg" };
        private static readonly Dictionary<string, string> _fileFormatMap = new()
        {
            {"docx", "docx:\"MS Word 2007 XML\""},
            {"csv", "csv:\"Text - txt - csv (StarCalc)\""},
            {"xlsx", "xlsx:\"Calc MS Excel 2007 XML\""},
            {"txt", "txt:\"Text\""},
            {"rtf", "rtf:\"Text (encoded):UTF8\""}
        };

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

                Process process = CreateConversionProcess(inputFilePath, outputFilePath);
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                var imageFiles = Directory.GetFiles(tempPath, $"{tempId}-*.{outputFormat}");
                if (process.ExitCode != 0 || !System.IO.File.Exists(outputFilePath))
                {
                    var outputNotExists = !System.IO.File.Exists(outputFilePath);
                    if (outputNotExists && outputFormat != "png")
                    {
                        return StatusCode(500, $"Conversion failed. Error: {error}");
                    }

                    if (!imageFiles.Any())
                    {
                        return StatusCode(500, $"Conversion failed. Error: {error}");
                    }
                }

                if (!imageFormats.Any(f => f == outputFormat))
                {
                    var outputBytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);
                    return File(outputBytes, _fileMimeTypeMap[outputFormat], $"converted.{outputFormat}");
                }

                string zipFileName = $"{tempId}_images.zip";
                string zipFilePath = Path.Combine(tempPath, zipFileName); 

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (string imgFile in imageFiles)
                        {
                            var fileInfo = new FileInfo(imgFile);
                            var entry = archive.CreateEntry(fileInfo.Name, CompressionLevel.Optimal); 

                            using (var entryStream = entry.Open())
                            using (var fileStream = new FileStream(imgFile, FileMode.Open, FileAccess.Read))
                            {
                                fileStream.CopyTo(entryStream);
                            }
                        }
                    }

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return File(memoryStream.ToArray(), "application/zip", zipFileName);
                }
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


        private Process CreateConversionProcess(string inputFilePath, string outputFilePath)
        {
            var inputFileFormat = Path.GetExtension(inputFilePath).Substring(1);
            var outputFormat = Path.GetExtension(outputFilePath).Substring(1);
            var isInputFilePdf = inputFileFormat.ToLower() == "pdf";
            Process process = new Process();

            if (outputFormat == "png" || outputFormat == "jpeg" || inputFileFormat == "png" || inputFileFormat == "jpeg")
            {
                process.StartInfo.FileName = "magick";
                process.StartInfo.Arguments = $"{inputFilePath} {outputFilePath}";
            }
            else
            {
                var outputDirectory = Path.GetDirectoryName(outputFilePath);
                _fileFormatMap.TryGetValue(outputFormat, out var outputFileFormat);
                outputFileFormat ??= outputFormat;
                process.StartInfo.FileName = "soffice";
                process.StartInfo.Arguments = $"--headless {(isInputFilePdf ? "--infilter=writer_pdf_import" : "")} --convert-to {outputFileFormat} \"{inputFilePath}\" --outdir \"{outputDirectory}\"";
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            return process;
        }

    }
}
