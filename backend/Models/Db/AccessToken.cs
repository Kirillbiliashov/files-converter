using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models.Db
{
    public class AccessToken: MongoDBDocument
    {
        [BsonElement("accessToken")]
        public string Token { get; set; }  

        [BsonElement("refreshToken")]
        public string RefreshToken { get; set; }

        [BsonElement("tokenExpiration")]
        public DateTime TokenExpiration { get; set; }

        [BsonElement("provider")]
        public string Provider { get; set; }

        [BsonElement("connectedAt")]
        [BsonIgnoreIfNull]
        public DateTime ConnectedAt { get; set; }


        [BsonElement("userId")]
        public ObjectId UserId { get; set; } 
    }
}