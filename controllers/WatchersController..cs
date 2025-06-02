using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRGame.Data;
using SignalRGame.Hubs;
using SignalRGame.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WatchersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<YouTubeChatHub> _hubContext;

        public WatchersController(ApplicationDbContext context, IHubContext<YouTubeChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // POST: /api/watchers
        [HttpPost]
        public async Task<IActionResult> AddWatcher([FromBody] Watcher watcher)
        {
            if (string.IsNullOrWhiteSpace(watcher.Username))
                return BadRequest("Username is required.");

            var existing = await _context.Watchers
                .FirstOrDefaultAsync(w => w.Username == watcher.Username);

            if (existing != null)
                return Conflict("Watcher already exists.");

            _context.Watchers.Add(watcher);
            await _context.SaveChangesAsync();

            return StatusCode(201);
        }

        // POST: /api/watchers/broadcast
        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastWatchers()
        {
            var watchers = await _context.Watchers.ToListAsync();
            await _hubContext.Clients.All.SendAsync("UpdateWatchers", watchers);
            return Ok();
        }
    }
}
