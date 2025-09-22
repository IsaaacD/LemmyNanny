using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LemmyNanny
{


    public class WebSocketConsumer(string sisterSiteWebSocketUrl)
    {
        private BlockingCollection<Post> postItems = new(100);
        private BlockingCollection<Comment> commentItems = new(100);
        public async Task ConnectAndReceive(CancellationToken cancellationToken = default)
        {
 
            try
            {
                {
                    ////Set connection
                    var connection = new HubConnectionBuilder()
                        .WithAutomaticReconnect()
                        .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
                        .WithUrl(sisterSiteWebSocketUrl)
                        .Build();

                    connection.On<List<string>>("Initial_Comments", (o) =>
                    {
                        Console.WriteLine("received initial Comments");
                        foreach (var item in o)
                        {
                            var deserialized = JsonSerializer.Deserialize<Comment>(item);
                            if(deserialized != null)
                                commentItems.Add(deserialized);
                        }
                        Console.WriteLine(o.ToString());
                    });
                    connection.On<List<string>>("Initial_Posts", (o) =>
                    {
                        Console.WriteLine("received initial Posts");
                        foreach (var item in o)
                        {
                            var deserialized = JsonSerializer.Deserialize<Post>(item);
                            if (deserialized != null)
                                postItems.Add(deserialized);
                        }
                        Console.WriteLine(o.ToString());
                    });
                    connection.On<string>("Posts_From_Lemmy", (o) =>
                    {
                        Console.WriteLine("received Post");
                        var deserialized = JsonSerializer.Deserialize<Post>(o);
                        if (deserialized != null)
                            postItems.Add(deserialized);
                        Console.WriteLine(o.ToString());
                    });

                    connection.On<string>("Comments_From_Lemmy", (o) =>
                    {
                        Console.WriteLine("received Comment");
                        var deserialized = JsonSerializer.Deserialize<Comment>(o);
                        if (deserialized != null)
                            commentItems.Add(deserialized);
                        Console.WriteLine(o.ToString());
                    });

                    await connection.StartAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {

            }
        }
    }

}
