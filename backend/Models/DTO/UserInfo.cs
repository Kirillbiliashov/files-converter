using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models.DTO
{

    [BsonIgnoreExtraElements]
    public class UserInfo
    {
        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("created")]
        public DateTime Created { get; set; }

        [BsonElement("lastLogin")]
        public DateTime LastLogin { get; set; }
        
        [BsonElement("providers")]
        public List<UserProvider> Providers {get; set;}
    }

    [BsonIgnoreExtraElements]
    public class UserProvider
    {

        [BsonElement("provider")]
        public string Provider { get; set; }
    }
}