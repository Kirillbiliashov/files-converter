using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace backend.BL.Converter
{
    public class LibreOfficeFileConverter : IFileConverter
    {

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
        };

        public async Task<ConversionResult> ConvertFile(string inputFilePath, string outputFilePath)
        {
            var inputFileFormat = Path.GetExtension(inputFilePath).Substring(1);
            var outputFormat = Path.GetExtension(outputFilePath).Substring(1);
            var isInputFilePdf = inputFileFormat.ToLower() == "pdf";
            string userProfilePath = CreateUniqueUserProfile();

            try
            {
                var outputDirectory = Path.GetDirectoryName(outputFilePath);
                _fileFormatMap.TryGetValue(outputFormat, out var outputFileFormat);
                outputFileFormat ??= outputFormat;

                Process process = new Process();
                process.StartInfo.FileName = "soffice";
                process.StartInfo.Arguments = $"--headless -env:UserInstallation=\"{userProfilePath}\" {(isInputFilePdf ? "--infilter=writer_pdf_import" : "")} --convert-to {outputFileFormat} \"{inputFilePath}\" --outdir \"{outputDirectory}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 || !System.IO.File.Exists(outputFilePath))
                {
                    throw new Exception($"Conversion failed. Error: {error}");
                }

                var bytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);
                var filename = Path.GetFileNameWithoutExtension(inputFilePath);
                return new ConversionResult
                {
                    OutputBytes = bytes,
                    MimeType = _fileMimeTypeMap[outputFormat],
                    Filename = $"{filename}.{outputFormat}"
                };
            }
            finally
            {
                DeleteUserProfile(userProfilePath);
            }
        }


        private string CreateUniqueUserProfile()
        {
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
            string userProfilePath = new Uri(tempDir.FullName).AbsoluteUri;
            return userProfilePath;
        }

        private void DeleteUserProfile(string userProfilePath)
        {
            Uri uri = new Uri(userProfilePath);
            string localPath = uri.LocalPath;

            if (Directory.Exists(localPath))
            {
                Directory.Delete(localPath, true);
            }
        }

    }
}