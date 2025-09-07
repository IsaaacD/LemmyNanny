using dotNETLemmy.API;
using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Enums;
using LemmyNanny.Interfaces;
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

            var sortType = builder.Configuration["SortType"] ?? "Active";
            var listingType = builder.Configuration["ListingType"] ?? "All";

            builder.Services.AddHostedService(provider =>
                new LemmyNannyWorker(provider.GetRequiredService<IHistoryManager>(), 
                provider.GetRequiredService<IPictrsManager>(),
                provider.GetRequiredService<IOllamaManager>(),
                provider.GetRequiredService<ILemmyManager>()));

            var dbName = builder.Configuration["SqliteDb"] // defaults to SqliteDb value, if not present makes a new db
                ?? $"LemmyNanny-{sortType}-{listingType}-{DateTime.Now.ToString("yyyy-MM-dd hh-mm")}.db";
            
            builder.Services.AddHttpClient(OllamaManager.CLIENT_NAME,
                client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration["OllamaUrl"] ?? throw new Exception("OllamaUrl not set"));
                    client.Timeout = TimeSpan.FromMinutes(10);
                });
            builder.Services.AddHttpClient(PictrsManager.CLIENT_NAME);
            builder.Services.AddSingleton<IPictrsManager, PictrsManager>(pro=> new PictrsManager(pro.GetRequiredService<IHttpClientFactory>()));

            builder.Services.AddSingleton<ILemmyManager>(provider => new LemmyManager(provider.GetRequiredService<ILemmyHttpClient>(), Enum.Parse<SortType>(sortType), Enum.Parse<ListingType>(listingType)));
            builder.Services.AddSingleton<IOllamaApiClient>(pro =>
            {
                var http = pro.GetRequiredService<IHttpClientFactory>();
                var client = new OllamaApiClient(http.CreateClient(OllamaManager.CLIENT_NAME))
                {
                    SelectedModel = builder.Configuration["OllamaModel"] ?? throw new Exception("OllamaModel not set")
                };
                return client;
            });
            builder.Services.AddSingleton<IOllamaManager>(pro => new OllamaManager(pro.GetRequiredService<IOllamaApiClient>(), builder.Configuration["Prompt"] ?? throw new Exception("Prompt not set")));
            builder.Services.AddSingleton<ILemmyHttpClient, LemmyHttpClient>(o=> 
                new LemmyHttpClient(new HttpClient { BaseAddress = new Uri(builder.Configuration["LemmyHost"] ?? throw new Exception("LemmyHost is not set")) })
                { 
                    BaseAddress = builder.Configuration["LemmyHost"] ?? throw new Exception("LemmyHost is not set"), 
                    Password = builder.Configuration["LemmyPassword"] ?? "",
                    Username = builder.Configuration["LemmyUserName"] ?? ""
                });
            builder.Services.AddSingleton<IHistoryManager>(o=> new HistoryManager(dbName));


            IHost host = builder.Build();
            var imageBytes = new[] { File.ReadAllBytes("LemmyNannyLogo.png") };
            
            foreach (var consoleImage in imageBytes.Select(bytes => new CanvasImage(bytes)))
            {
                consoleImage.MaxWidth = 60;
                AnsiConsole.Write(consoleImage);
            }
            AnsiConsole.WriteLine("Welcome to LemmyNanny!");
            host.Run();
        }
    }
}
