using LemmyNanny.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System.Text.Json;

namespace LemmyNanny
{

    /// <summary>
    /// Connects to LemmyNannyWeb to retrieve websocket connection from LemmyWebhooks (PHP docker container loaded on Lemmy server)
    /// </summary>
    /// <param name="sisterSiteWebSocketUrl"></param>
    /// <param name="lemmyManager"></param>
    public class WebSocketConsumer(string sisterSiteWebSocketUrl, ILemmyManager lemmyManager) : BackgroundService, ILemmyConsumer
    {

        private readonly HubConnection connection = new HubConnectionBuilder()
             .WithAutomaticReconnect()
             .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
             .WithUrl(sisterSiteWebSocketUrl)
             .Build();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                RegisterWebSocketConnections();
                await connection.StartAsync();
                connection.Closed += async (ex) =>
                {
                    await connection.StartAsync();
                    AnsiConsole.WriteLine($"WebSocket Reconnect: {ex.Message}");
                };

            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {

            }
        }

        private void RegisterWebSocketConnections()
        {
            connection.On<List<string>>("Initial_Comments", async (o) =>
            {
                Console.WriteLine("received initial Comments");
                foreach (var item in o)
                {
                    var deserialized = JsonSerializer.Deserialize<Comment>(item);
                    if (deserialized != null)
                    {
                        CommentAndPostBucket.CommentItems.TryAdd(await lemmyManager.GetComment(deserialized.data.id));
                    }

                }
                Console.WriteLine(o.ToString());
            });
            connection.On<List<string>>("Initial_Posts", async (o) =>
            {
                Console.WriteLine("received initial Posts");
                foreach (var item in o)
                {
                    var deserialized = JsonSerializer.Deserialize<Post>(item);
                    if (deserialized != null)
                        CommentAndPostBucket.PostItems.TryAdd(await lemmyManager.GetPost(deserialized.data.id));
                }
                Console.WriteLine(o.ToString());
            });
            connection.On<string>("Posts_From_Lemmy", async (o) =>
            {
                Console.WriteLine("received Post");
                var deserialized = JsonSerializer.Deserialize<Post>(o);
                if (deserialized != null)
                    CommentAndPostBucket.PostItems.TryAdd(await lemmyManager.GetPost(deserialized.data.id));
                Console.WriteLine(o.ToString());
            });

            connection.On<string>("Comments_From_Lemmy", async (o) =>
            {
                Console.WriteLine("received Comment");
                var deserialized = JsonSerializer.Deserialize<Comment>(o);
                if (deserialized != null)
                    CommentAndPostBucket.CommentItems.TryAdd(await lemmyManager.GetComment(deserialized.data.id));
                Console.WriteLine(o.ToString());
            });
        }
    }

}
