using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models.Db
{
    public class FileInteraction: MongoDBDocument
    {
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }  

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("extension")]
        public string Extension { get; set; }
        
        [BsonElement("size")]
        public int Size { get; set; } 

        [BsonElement("date")]
        public DateTime Date { get; set; }
    }
}