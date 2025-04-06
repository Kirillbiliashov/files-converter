using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models.Db
{
    public class User: MongoDBDocument
    {
        [BsonElement("username")]
        public string Username { get; set; }  

        [BsonElement("email")]
        public string Email { get; set; }
        
        [BsonElement("passwordHash")]
        [BsonIgnoreIfNull]
        public string PasswordHash { get; set; } 

        [BsonElement("created")]
        public DateTime Created { get; set; }

        [BsonElement("lastLogin")]
        public DateTime LastLogin { get; set; }

        [BsonElement("deleteFiles")]
        public bool DeleteFilesAutomatically { get; set; }
    }
}