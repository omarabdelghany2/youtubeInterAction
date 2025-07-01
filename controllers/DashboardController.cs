using Microsoft.AspNetCore.Mvc;
using SignalRGame.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRGame.Data;
using SignalRGame.Hubs;
using SignalRGame.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Globalization;
namespace SignalRGame.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }
            [Authorize]
        [HttpGet("watchers")]
        public async Task<IActionResult> GetWatchers(
            int page = 1,
            int pageSize = 10,
            string? sortBy = null,
            string? prefix = null)
        {
            IQueryable<Watcher> query = _context.Watchers;

            // Filter by username prefix
            if (!string.IsNullOrEmpty(prefix))
            {
                query = query.Where(w => w.Username.StartsWith(prefix));
            }

            // Custom sorting by interaction match first
            if (!string.IsNullOrEmpty(sortBy))
            {
                query = query
                    .OrderByDescending(w => w.interaction.ToLower() == sortBy.ToLower()) // Matches go to top
                    .ThenBy(w => w.interaction) // Group the rest alphabetically by interaction
                    .ThenBy(w => w.Username);   // Optional: sort by name within group
            }
            else
            {
                // Default sort by Username
                query = query.OrderBy(w => w.Username);
            }

            // Pagination
            var totalItems = await query.CountAsync();
            var watchers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                page,
                pageSize,
                totalItems,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                data = watchers
            });
        }


        [Authorize]
        [HttpDelete("watchers/{id}")]
        public async Task<IActionResult> DeleteWatcher(int id)
        {
            var watcher = await _context.Watchers.FindAsync(id);

            if (watcher == null)
            {
                return NotFound($"Watcher with ID {id} not found.");
            }

            _context.Watchers.Remove(watcher);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [Authorize]
        [HttpDelete("watchers")]
        public async Task<IActionResult> DeleteCommentOnlyWatchers()
        {
            var commentWatchers = _context.Watchers
                .Where(w => w.interaction.ToLower() == "comment")
                .ToList();

            if (!commentWatchers.Any())
            {
                return NotFound("No watchers with interaction 'comment' found to delete.");
            }

            _context.Watchers.RemoveRange(commentWatchers);
            await _context.SaveChangesAsync();

            return NoContent();
        }





        [HttpGet("watchers/plaintext")]
        public async Task<IActionResult> GetAllWatchersPlainText()
        {
            try
            {
                // Fetch watchers from DB
                var watchers = await _context.Watchers.ToListAsync();

                // Join usernames comma + space separated, trimmed
                var namesInput = string.Join(" ,", watchers.OrderBy(w => w.Username.Trim(), StringComparer.Create(new CultureInfo("en-US"), true)).Select(w => w.Username.Trim()));

                var apiDevKey = "BhNtcNvOS1Mys6TidCZuMcSrIf99-gV4";
                var pastebinPostUrl = "https://pastebin.com/api/api_post.php";

                var postData = new Dictionary<string, string>
                {
                    { "api_dev_key", apiDevKey },
                    { "api_option", "paste" },
                    { "api_paste_code", namesInput },
                    { "api_paste_format", "text" },
                    { "api_paste_private", "1" } // unlisted
                };

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync(pastebinPostUrl, new FormUrlEncodedContent(postData));
                response.EnsureSuccessStatusCode();

                var pasteUrl = await response.Content.ReadAsStringAsync();

                if (!pasteUrl.StartsWith("http"))
                {
                    return StatusCode(500, "Error from Pastebin: " + pasteUrl);
                }

                var pasteId = pasteUrl.Split('/').Last();
                var rawUrl = $"https://pastebin.com/raw/{pasteId}";

                Response.Headers["Access-Control-Allow-Origin"] = "*";
                Response.Headers["Cache-Control"] = "max-age=14400";
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                Response.Headers["X-Frame-Options"] = "DENY";
                Response.Headers["X-Xss-Protection"] = "1; mode=block";

                return Content(rawUrl, "text/plain; charset=utf-8");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving watchers and posting to Pastebin.");
                return StatusCode(500, "Internal server error while retrieving watchers.");
            }
        }



        // [HttpGet("watchers/plaintext")]
        // public async Task<IActionResult> GetAllWatchersPlainText()
        // {
        //     try
        //     {
        //         // Fetch watchers from DB
        //         var watchers = await _context.Watchers.ToListAsync();

        //         // Join usernames comma + space separated, trimmed
        //         var namesInput = string.Join(", ", watchers.OrderBy(w => w.Username.Trim(), StringComparer.Create(new CultureInfo("en-US"), true)).Select(w => w.Username.Trim()));

        //         var apiDevKey = "BhNtcNvOS1Mys6TidCZuMcSrIf99-gV4";
        //         var pastebinPostUrl = "https://pastebin.com/api/api_post.php";

        //         var postData = new Dictionary<string, string>
        //         {
        //             { "api_dev_key", apiDevKey },
        //             { "api_option", "paste" },
        //             { "api_paste_code", namesInput },
        //             { "api_paste_format", "text" },
        //             { "api_paste_private", "1" } // unlisted
        //         };

        //         // Setup proxy
        //         var proxy = new WebProxy("65.108.203.37",18080)
        //         {
        //             // Uncomment and fill in if proxy requires authentication:
        //             // Credentials = new NetworkCredential("username", "password")
        //         };

        //         var httpClientHandler = new HttpClientHandler()
        //         {
        //             Proxy = proxy,
        //             UseProxy = true,
        //         };

        //         using var httpClient = new HttpClient(httpClientHandler);

        //         var response = await httpClient.PostAsync(pastebinPostUrl, new FormUrlEncodedContent(postData));

        //         // Log status code for debugging
        //         _logger.LogInformation("Pastebin POST response status: {StatusCode}", response.StatusCode);

        //         response.EnsureSuccessStatusCode();

        //         var pasteUrl = await response.Content.ReadAsStringAsync();

        //         if (!pasteUrl.StartsWith("http"))
        //         {
        //             return StatusCode(500, "Error from Pastebin: " + pasteUrl);
        //         }

        //         var pasteId = pasteUrl.Split('/').Last();
        //         var rawUrl = $"https://pastebin.com/raw/{pasteId}";

        //         Response.Headers["Access-Control-Allow-Origin"] = "*";
        //         Response.Headers["Cache-Control"] = "max-age=14400";
        //         Response.Headers["X-Content-Type-Options"] = "nosniff";
        //         Response.Headers["X-Frame-Options"] = "DENY";
        //         Response.Headers["X-Xss-Protection"] = "1; mode=block";

        //         return Content(rawUrl, "text/plain; charset=utf-8");
        //     }
        //     catch (HttpRequestException httpEx)
        //     {
        //         _logger.LogError(httpEx, "HTTP error during posting to Pastebin, possibly proxy issue.");
        //         return StatusCode(502, "Bad Gateway: Proxy error or Pastebin unreachable.");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error retrieving watchers and posting to Pastebin.");
        //         return StatusCode(500, "Internal server error while retrieving watchers.");
        //     }
        // }




    }
}
