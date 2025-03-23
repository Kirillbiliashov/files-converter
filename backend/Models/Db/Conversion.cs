using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models.Db
{
    public class Conversion: MongoDBDocument
    {
        [BsonElement("timeMsecs")]
        public long TimeMsecs { get; set; }  

        [BsonElement("inputFormat")]
        public string InputFormat { get; set; }

        [BsonElement("outputFormat")]
        public string OutputFormat { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("filename")]
        [BsonIgnoreIfNull]
        public string Filename { get; set; }

        [BsonElement("fileSize")]
        [BsonIgnoreIfNull]
        public long FileSize { get; set; }

        [BsonElement("userId")]
        public ObjectId UserId { get; set; } 

        [BsonElement("outputUrl")]
        public string OutputUrl { get; set; } 

        [BsonElement("date")]
        public DateTime Date { get; set; }
    }
}