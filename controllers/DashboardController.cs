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

    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
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



        // 5. Get all watchers in plain text format
        [HttpGet("watchers/plaintext")]
        public async Task<IActionResult> GetAllWatchersPlainText()
        {
            var watchers = await _context.Watchers.ToListAsync();

            var lines = watchers.Select(w =>
                $"{w.Username}");

            var result = string.Join("\n", lines);

            return Content(result, "text/plain");
        }
    }
}
