using Microsoft.AspNetCore.Mvc;
using SignalRGame.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SignalRGame.Models;

namespace SignalRGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamlabsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StreamlabsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/streamlabs/token
        [HttpGet("token")]
        public async Task<IActionResult> GetToken()
        {
            var token = await _context.StreamlabsTokens.FirstOrDefaultAsync();
            if (token == null)
                return NotFound();

            return Ok(new { token = token.Token });
        }
    }
}
