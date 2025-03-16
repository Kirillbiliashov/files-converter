using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models.Db
{
    public abstract class MongoDBDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonIgnore]
        public string IdInternal
        {
            get => Id.ToString();
        }
    }
}