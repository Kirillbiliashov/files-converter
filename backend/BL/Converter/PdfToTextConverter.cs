using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace backend.BL.Converter
{
    public class PdfToTextConverter : IFileConverter
    {
        public async Task<ConversionResult> ConvertFile(string inputFilePath, string outputFilePath)
        {
            var outputFormat = Path.GetExtension(outputFilePath).Substring(1).Trim().ToLower();
            var tempPath = Path.GetDirectoryName(inputFilePath);
            var tempId = Path.GetFileName(inputFilePath).Split(".").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(tempId) || string.IsNullOrWhiteSpace(tempPath))
            {
                throw new Exception($"Couldn't extract input file name and path");
            }

            Process process = new Process();
            process.StartInfo.FileName = "pdftotext";
            process.StartInfo.Arguments = $"{inputFilePath} {outputFilePath}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var bytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);
            var filename = Path.GetFileNameWithoutExtension(outputFilePath);
            return new ConversionResult
            {
                OutputBytes = bytes,
                MimeType = "text/plain",
                Filename = $"{filename}.{outputFormat}"
            };
        }
    }
}