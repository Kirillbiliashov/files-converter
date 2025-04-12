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
using MongoDB.Driver;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardApiController : ControllerBase
    {

        private readonly IConversionRepository _conversionRepository;
        private readonly IFileInteractionsRepository _fileInteractionsRepository;

        public DashboardApiController(
            IConversionRepository conversionRepository,
            IFileInteractionsRepository fileInteractionsRepository)
        {
            _conversionRepository = conversionRepository;
            _fileInteractionsRepository = fileInteractionsRepository;
        }


        [Authorize]
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardData()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var userConversions = await _conversionRepository.GetConversions(userId);
            var userFileInteractions = await _fileInteractionsRepository.GetFileInteractions(userId);

            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var previousMonth = currentMonth.AddMonths(-1);

            var downloads = userFileInteractions.Where(i => i.Type == "download");
            var uploads = userFileInteractions.Where(i => i.Type == "upload");

            var conversionAnalytics = userConversions.GroupBy(c => (c.InputFormat, c.OutputFormat))
            .ToDictionary(g => $"{g.Key.InputFormat} → {g.Key.OutputFormat}", g => g.Count());

            var successfulConversions = userConversions.Where(c => c.Status == "success");

            return Ok(new
            {
                downloaded = new
                {
                    total = downloads.Count(),
                    difference = GetMonthIncrease(downloads.Select(d => d.Date))
                },
                uploaded = new
                {
                    total = uploads.Count(),
                    difference = GetMonthIncrease(uploads.Select(d => d.Date))
                },
                conversions = new
                {
                    total = userConversions.Count(),
                    difference = GetMonthIncrease(userConversions.Select(d => d.Date))
                },
                successRate = new
                {
                    total = (double)successfulConversions.Count() / userConversions.Count * 100,
                    difference = GetMonthIncrease(successfulConversions.Select(d => d.Date))
                },
                analytics = conversionAnalytics,
                activity = userConversions
            });
        }

        private double? GetMonthIncrease(IEnumerable<DateTime> dates)
        {

            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var previousMonth = currentMonth.AddMonths(-1);
            var datesThisMonth = dates.Where(d => d.Year == currentMonth.Year && d.Month == currentMonth.Month).Count();
            var datesLastMonth = dates.Where(d => d.Year == previousMonth.Year && d.Month == previousMonth.Month).Count();

            Console.WriteLine($"Dates this month: {datesThisMonth}, dates last month: {datesLastMonth}");

            if (datesLastMonth == 0)
            {
                return null;
            }
            return (datesThisMonth - datesLastMonth) / (double)datesLastMonth * 100;
        }

    }
}