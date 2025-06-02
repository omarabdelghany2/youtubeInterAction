using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;
using SignalRGame.Hubs;

namespace SignalRGame.Services
{
    public class YouTubeChatListenerService
    {
        private readonly IHubContext<YouTubeChatHub> _hubContext;
        private readonly string _apiKey;

        private readonly ApplicationDbContext _context;

        public YouTubeChatListenerService(IHubContext<YouTubeChatHub> hubContext, string apiKey, ApplicationDbContext context)
        {
            _hubContext = hubContext;
            _apiKey = apiKey;
            _context = context;
        }


        public async Task StartListening(string videoId, IHubCallerClients clients, CancellationToken cancellationToken)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _apiKey,
                ApplicationName = "MyYouTubeChatListenerApp"
            });

            // Get liveChatId from video
            var videoRequest = youtubeService.Videos.List("liveStreamingDetails");
            videoRequest.Id = videoId;
            var videoResponse = await videoRequest.ExecuteAsync(cancellationToken);

            var liveChatId = videoResponse.Items?[0]?.LiveStreamingDetails?.ActiveLiveChatId;

            if (liveChatId == null)
            {
                await clients.Caller.SendAsync("ReceiveMessage", "No active live chat for this video.");
                return;
            }

            string nextPageToken = "";

            while (!cancellationToken.IsCancellationRequested)
            {
                var chatRequest = youtubeService.LiveChatMessages.List(liveChatId, "snippet,authorDetails");
                chatRequest.PageToken = nextPageToken;
                chatRequest.MaxResults = 200;

                var chatResponse = await chatRequest.ExecuteAsync(cancellationToken);

                foreach (var chatMessage in chatResponse.Items)
                {
                    string author = chatMessage.AuthorDetails.DisplayName;
                    string message = chatMessage.Snippet.DisplayMessage;

                    // Check if the watcher already exists
                    var existingWatcher = await _context.Watchers.FirstOrDefaultAsync(w => w.Username == author);

                    if (existingWatcher == null)
                    {
                        var newWatcher = new watcher
                        {
                            Username = author,
                            interaction = "comment",
                            platform = "YouTube"
                        };

                        _context.Watchers.Add(newWatcher);
                        await _context.SaveChangesAsync();

                        // Send updated watcher list
                        var watchers = await _context.Watchers.ToListAsync(cancellationToken);
                        await _hubContext.Clients.All.SendAsync("UpdateWatchers", watchers, cancellationToken);
                    }


                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", $"{author}: {message}", cancellationToken);
                }


                nextPageToken = chatResponse.NextPageToken;

                await Task.Delay(5000, cancellationToken);
            }
        }
    }
}
