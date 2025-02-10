using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.BL.Converter
{
    public class ConversionResult
    {
        public byte[] OutputBytes { get; set; }

        public string MimeType { get; set; }

        public string Filename { get; set; }
    }
}