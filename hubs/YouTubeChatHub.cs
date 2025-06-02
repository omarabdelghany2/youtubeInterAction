using Microsoft.AspNetCore.SignalR;
using SignalRGame.Services;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SignalRGame.Data;
using SignalRGame.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;


namespace SignalRGame.Hubs
{
    public class YouTubeChatHub : Hub
    {
        private readonly YouTubeChatListenerService _listenerService;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<YouTubeChatHub> _hubContext;

        private static string _videoId = null;
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static Task _listeningTask = Task.CompletedTask;

        public YouTubeChatHub(
            YouTubeChatListenerService listenerService,
            ApplicationDbContext context,
            IHubContext<YouTubeChatHub> hubContext)
        {
            _listenerService = listenerService;
            _context = context;
            _hubContext = hubContext;
        }

        [Authorize]
        public async Task SetVideoId(string videoId, string streamLabsToken)
        {
            _videoId = videoId;

            var existingToken = _context.StreamlabsTokens.FirstOrDefault();

            if (existingToken != null)
            {
                existingToken.Token = streamLabsToken;
                _context.StreamlabsTokens.Update(existingToken);
            }
            else
            {
                var newToken = new StreamlabsToken
                {
                    Token = streamLabsToken
                };
                _context.StreamlabsTokens.Add(newToken);
            }

            await _context.SaveChangesAsync();

            var token = _context.StreamlabsTokens.FirstOrDefault()?.Token;

            // Use Clients.Caller instead of _hubContext.Clients.Caller
            await Clients.Caller.SendAsync("StreamlabsTokenUpdated", new
            {
                message = new
                {
                    videoId = _videoId,
                    token = token
                }
            });

            // Automatically start listener for the same client
            await MakeListener();
        }

        [Authorize]
        public async Task MakeListener()
        {
            Console.WriteLine("MakeListener called");
            Console.WriteLine(_videoId);

            if (string.IsNullOrEmpty(_videoId))
            {
                Console.WriteLine("Video ID is null or empty");
                return;
            }

            Console.WriteLine("Getting watchers");
            var watchers = await _context.Watchers.ToListAsync();
            await _hubContext.Clients.All.SendAsync("UpdateWatchers", watchers);
            Console.WriteLine($"Fetched {watchers.Count} watchers");

            Console.WriteLine("Checking old task");
            if (_listeningTask != null)
            {
                _cts.Cancel();
                Console.WriteLine("Old task canceled");

                try { await _listeningTask; } catch { }

                _cts.Dispose();
            }

            _cts = new CancellationTokenSource();
            Console.WriteLine("Starting new listener");
            _listeningTask = Task.Run(() => _listenerService.StartListening(_videoId, Clients, _cts.Token));
            Console.WriteLine("Listener task assigned");

            var token = _context.StreamlabsTokens.FirstOrDefault()?.Token;

            await Clients.Caller.SendAsync("StreamlabsTokenUpdated", new
            {
                message = new
                {
                    videoId = _videoId,
                    token = token
                }
            });
        }






    }
}
