using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.BL.Converter
{
    public interface IFileConverter
    {
        public Task<ConversionResult> ConvertFile(string inputFilePath, string outputFilePath);
    }
}