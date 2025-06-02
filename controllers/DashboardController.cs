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
namespace SignalRGame.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("watchers")]
        public async Task<IActionResult> GetWatchers(
            int page = 1,
            int pageSize = 10,
            string? sortBy = null,
            string? prefix = null)
        {
            IQueryable<Watcher> query = _context.Watchers;

            // Filter
            if (!string.IsNullOrEmpty(prefix))
            {
                query = query.Where(w => w.Username.StartsWith(prefix));
            }

            // Sort
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "interaction":
                        query = query.OrderBy(w => w.interaction);
                        break;
                    case "username":
                        query = query.OrderBy(w => w.Username);
                        break;
                    case "platform":
                        query = query.OrderBy(w => w.platform);
                        break;
                    default:
                        return BadRequest("Unsupported sort field.");
                }
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


        // 2. Delete all watchers
        [HttpDelete("watchers")]
        public async Task<IActionResult> DeleteAllWatchers()
        {
            _context.Watchers.RemoveRange(_context.Watchers);
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // 5. Get all watchers in plain text format
        [HttpGet("watchers/plaintext")]
        public async Task<IActionResult> GetAllWatchersPlainText()
        {
            var watchers = await _context.Watchers.ToListAsync();

            var lines = watchers.Select(w =>
                $"ID: {w.Id}, Username: {w.Username}, Interaction: {w.interaction}, Platform: {w.platform}");

            var result = string.Join("\n", lines);

            return Content(result, "text/plain");
        }
    }
}
