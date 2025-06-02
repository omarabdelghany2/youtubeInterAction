using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;
using SignalRGame.Hubs;
using SignalRGame.Data;

using Microsoft.EntityFrameworkCore;  // for async EF methods
using SignalRGame.Data;                // for ApplicationDbContext
using SignalRGame.Models;              // for watcher class
using System.Linq;                     // for LINQ extension methods

namespace SignalRGame.Services
{
    public class YouTubeChatListenerService
    {
        private readonly IHubContext<YouTubeChatHub> _hubContext;
        private readonly string _apiKey;

        private readonly ApplicationDbContext _context;

        private readonly IServiceScopeFactory _scopeFactory;

        public YouTubeChatListenerService(
            IHubContext<YouTubeChatHub> hubContext,
            string apiKey,
            ApplicationDbContext context,
            IServiceScopeFactory scopeFactory)   // Add this
        {
            _hubContext = hubContext;
            _apiKey = apiKey;
            _context = context;  // Ideally only for short-lived use, or remove this
            _scopeFactory = scopeFactory;
        }

        public async Task StartListening(string videoId, IHubCallerClients clients, CancellationToken cancellationToken)
        {
            try
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
                    Console.WriteLine("No active live chat found.");
                    return;
                }

                Console.WriteLine($"Live chat ID: {liveChatId}");
                string nextPageToken = "";



                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var chatRequest = youtubeService.LiveChatMessages.List(liveChatId, "snippet,authorDetails");
                        chatRequest.PageToken = nextPageToken;
                        chatRequest.MaxResults = 200;

                        var chatResponse = await chatRequest.ExecuteAsync(cancellationToken);

                        foreach (var chatMessage in chatResponse.Items)
                        {
                            string author = chatMessage.AuthorDetails.DisplayName;
                            string message = chatMessage.Snippet.DisplayMessage;

                            Console.WriteLine($"Message from {author}: {message}");

                            using var scope = _scopeFactory.CreateScope();
                            var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                            var existingWatcher = await scopedContext.Watchers.FirstOrDefaultAsync(w => w.Username == author);

                            if (existingWatcher == null)
                            {
                                var newWatcher = new Watcher
                                {
                                    Username = author,
                                    interaction = "comment",
                                    platform = "YouTube"
                                };

                                scopedContext.Watchers.Add(newWatcher);
                                await scopedContext.SaveChangesAsync();

                                var watchers = await scopedContext.Watchers.ToListAsync();
                                await _hubContext.Clients.All.SendAsync("UpdateWatchers", watchers);
                            }
                        }

                        nextPageToken = chatResponse.NextPageToken ?? "";

                        await Task.Delay(5000, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("Listening task cancelled.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during chat polling: {ex.Message}");
                        await Task.Delay(5000, cancellationToken);
                    }
                }



            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in StartListening: {ex.Message}");
                await clients.Caller.SendAsync("ReceiveMessage", $"Error in StartListening: {ex.Message}");
            }
        }

    }
}
