using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models.Db
{
    public class User
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }  

        [BsonElement("email")]
        public string Email { get; set; }
        
        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } 

        [BsonElement("created")]
        public DateTime Created { get; set; }
    }
}