using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.BL.Converter
{
    public class DocxToImageConverter : IFileConverter
    {
        private readonly ImageMagickFileConverter _imageConverter;
        private readonly LibreOfficeFileConverter _pdfConverter;

        public DocxToImageConverter(ImageMagickFileConverter imageConverter, LibreOfficeFileConverter pdfConverter)
        {
            _imageConverter = imageConverter;
            _pdfConverter = pdfConverter;
        }

        public async Task<ConversionResult> ConvertFile(string inputFilePath, string outputFilePath)
        {
            string tempPdfPath = Path.ChangeExtension(inputFilePath, "pdf");
            try
            {
                await _pdfConverter.ConvertFile(inputFilePath, tempPdfPath);
                var conversionResult = await _imageConverter.ConvertFile(tempPdfPath, outputFilePath);
                return conversionResult;
            }
            finally
            {
                if (File.Exists(tempPdfPath))
                {
                    File.Delete(tempPdfPath);
                }
            }

        }

    }
}