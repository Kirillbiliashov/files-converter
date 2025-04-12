using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using backend.Models.Db;
using backend.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatsApiController : ControllerBase
    {
        private readonly IFileInteractionsRepository _fileInteractionsRepository;

        public StatsApiController(IFileInteractionsRepository fileInteractionsRepository)
        {
            _fileInteractionsRepository = fileInteractionsRepository;
        }

        [Authorize]
        [HttpPost("add")]
        public async Task<IActionResult> UpdateUploadsStats([FromBody] List<StatsBody> body)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            await _fileInteractionsRepository.AddInteractions(body.Select(s => new FileInteraction
            {
                UserId = ObjectId.Parse(userId),
                Date = DateTime.UtcNow,
                Type = s.Type,
                Extension = Path.GetExtension(s.Name).Substring(1),
                Size = s.Size,
                Name = s.Name
            }));

            return Ok();
        }
    }

    public class StatsBody
    {
        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("size")]
        public int Size { get; set; }
    }

}