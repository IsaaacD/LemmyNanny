using dotNETLemmy.API;
using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OllamaSharp;
using Spectre.Console;

namespace LemmyNanny
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            var sortType = builder.Configuration["SortType"] ?? "Hot";
            var listingType = builder.Configuration["ListingType"] ?? "All";
            builder.Services.AddHostedService(provider =>
                new LemmyNannyWorker(provider.GetRequiredService<HistoryManager>(), 
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<IOllamaApiClient>(),
                builder.Configuration["Prompt"]?? throw new Exception("Prompt not set"),
                provider.GetRequiredService<LemmyManager>()));
            var dbName = builder.Configuration["SqliteDb"] // defaults to SqliteDb value, if not present makes a new db
                ?? $"LemmyNanny-{sortType}-{listingType}-{DateTime.Now.ToString("yyyy-MM-dd hh-mm")}.db";
            
            builder.Services.AddHttpClient("OllamaClient",
                client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration["OllamaUrl"] ?? throw new Exception("OllamaUrl not set"));
                    client.Timeout = TimeSpan.FromMinutes(10);
                });
            builder.Services.AddHttpClient("PictrsClient");

            builder.Services.AddSingleton(provider => new LemmyManager(provider.GetRequiredService<ILemmyHttpClient>(), Enum.Parse<SortType>(sortType), Enum.Parse<ListingType>(listingType)));
            builder.Services.AddSingleton<IOllamaApiClient>(pro =>
            {
                var http = pro.GetRequiredService<IHttpClientFactory>();
                var client =  new OllamaApiClient(http.CreateClient("OllamaClient"));
                client.SelectedModel = builder.Configuration["OllamaModel"] ?? throw new Exception("OllamaModel not set");
                return client;
            });

            builder.Services.AddSingleton<ILemmyHttpClient, LemmyHttpClient>(o=> 
                new LemmyHttpClient(new HttpClient { BaseAddress = new Uri(builder.Configuration["LemmyHost"] ?? throw new Exception("LemmyHost is not set")) })
                { 
                    BaseAddress = builder.Configuration["LemmyHost"] ?? throw new Exception("LemmyHost is not set"), 
                    Password = builder.Configuration["LemmyPassword"] ?? "",
                    Username = builder.Configuration["LemmyUserName"] ?? ""
                });
            builder.Services.AddSingleton(o=> new HistoryManager(dbName));


            IHost host = builder.Build();
            var imageBytes = new[] { File.ReadAllBytes("LemmyNannyLogo.png") };
            
            foreach (var consoleImage in imageBytes.Select(bytes => new CanvasImage(bytes)))
            {
                consoleImage.MaxWidth = 40;
                AnsiConsole.Write(consoleImage);
            }
            AnsiConsole.WriteLine("Welcome to LemmyNanny!");
            host.Run();
        }
    }
}
