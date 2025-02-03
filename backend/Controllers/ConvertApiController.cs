using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/convert")]
    public class ConvertApiController : ControllerBase
    {
        private static readonly Dictionary<string, string> _fileFormatMap = new()
        {
            {"docx", "docx:\"MS Word 2007 XML\""},
            {"csv", "csv:\"Text - txt - csv (StarCalc)\""},
            {"xlsx", "xlsx:\"Calc MS Excel 2007 XML\""},
            {"txt", "txt:\"Text\""}
        };

        private static readonly Dictionary<string, string> _fileMimeTypeMap = new ()
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
        };

        [HttpPost("")]
        public async Task<IActionResult> ConvertFile(IFormFile file, [FromForm] string outputFormat)
        {
            var inputFileExtension = Path.GetExtension(file.FileName);
            var isInputFilePdf = inputFileExtension.ToLower() == ".pdf";
            var tempPath = Path.GetTempPath();
            var inputFilePath = Path.Combine(tempPath, $"{Guid.NewGuid()}{inputFileExtension}");
            var outputDirectory = tempPath;
            var outputFilePath = inputFilePath.Replace(inputFileExtension, $".{outputFormat}");
            _fileFormatMap.TryGetValue(outputFormat, out var outputFileFormat);
            outputFileFormat ??= outputFormat;

            using (var stream = new FileStream(inputFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Process process = new Process();
            process.StartInfo.FileName = "soffice";
            process.StartInfo.Arguments = $"--headless {(isInputFilePdf ? "--infilter=writer_pdf_import" : "")} --convert-to {outputFileFormat} \"{inputFilePath}\" --outdir \"{outputDirectory}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            Console.WriteLine($"Process command: {process.StartInfo.Arguments}");

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            Console.WriteLine($"Output: {output}, error: {error}");

            if (process.ExitCode != 0 || !System.IO.File.Exists(outputFilePath))
            {
                return StatusCode(500, $"Conversion failed. Error: {error}");
            }

            var outputBytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);
            return File(outputBytes, _fileMimeTypeMap[outputFormat], $"converted.{outputFormat}");
        }

    }
}
