using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace backend.BL.Converter
{
    public class ImageMagickFileConverter : IFileConverter
    {
        private static readonly Dictionary<string, string> _fileMimeTypeMap = new()
        {
            { "png", "image/png" },
            { "jpg", "image/jpeg" },
            { "jpeg", "image/jpeg" }
        };

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
            process.StartInfo.FileName = "magick";
            process.StartInfo.Arguments = $"-density 300 {inputFilePath} {outputFilePath}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var imageFiles = Directory.GetFiles(tempPath, $"{tempId}-*.{outputFormat}");
            if (process.ExitCode != 0 || (!imageFiles.Any() && !System.IO.File.Exists(outputFilePath)))
            {
                throw new Exception($"Conversion failed. Error: {error}");
            }

            if (!imageFiles.Any())
            {
                var bytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);

                return new ConversionResult
                {
                    OutputBytes = bytes,
                    MimeType = _fileMimeTypeMap[outputFormat],
                    Filename = $"converted.{outputFormat}"
                };
            }

            var zipFileName = $"{tempId}_images.zip";
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
                return new ConversionResult
                {
                    OutputBytes = memoryStream.ToArray(),
                    MimeType = "application/zip",
                    Filename = zipFileName
                };
            }

        }

    }
}