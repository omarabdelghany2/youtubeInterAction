using Microsoft.AspNetCore.SignalR;
using SignalRGame.Services;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SignalRGame.Hubs
{
    [Authorize]
    public class YouTubeChatHub : Hub
    {
        private readonly YouTubeChatListenerService _listenerService;

        private static string _videoId = null;
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static Task _listeningTask = Task.CompletedTask;

        public YouTubeChatHub(YouTubeChatListenerService listenerService)
        {
            _listenerService = listenerService;
        }

        public Task SetVideoId(string videoId)
        {
            _videoId = videoId;
            return Task.CompletedTask;
        }

        public async Task MakeListener()
        {
            if (string.IsNullOrEmpty(_videoId))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "Video ID not set. Please call SetVideoId first.");
                return;
            }

            if (!_listeningTask.IsCompleted)
            {
                _cts.Cancel();

                try
                {
                    await _listeningTask;  // Wait for the old listener to finish cleanly
                }
                catch (TaskCanceledException)
                {
                    // Expected cancellation, no need to log
                }
                catch (Exception ex)
                {
                    // Optional: log unexpected exceptions here if desired
                }

                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }

            _listeningTask = _listenerService.StartListening(_videoId, Clients, _cts.Token);

            try
            {
                await _listeningTask;
            }
            catch (TaskCanceledException)
            {
                // Expected if this listener is cancelled (e.g., client disconnects)
            }
            catch (Exception ex)
            {
                // Optional: handle/log unexpected exceptions
            }
        }

    }
}
