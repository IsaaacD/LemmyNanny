using dotNETLemmy.API;
using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Enums;
using LemmyNanny.Interfaces;
using Microsoft.Extensions.Configuration;
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
            var model = builder.Configuration["OllamaModel"];
            var useLemmyWebhooks = builder.Configuration["LemmyWebhooks"];

            if (!string.IsNullOrEmpty(useLemmyWebhooks))
            {
                builder.Services.AddSingleton<ILemmyConsumer, WebSocketConsumer>(prov => new WebSocketConsumer(useLemmyWebhooks, prov.GetRequiredService<ILemmyManager>()));
                builder.Services.AddHostedService(prov => new WebSocketConsumer(useLemmyWebhooks, prov.GetRequiredService<ILemmyManager>()));
            }
            else
            {
                builder.Services.AddSingleton<ILemmyConsumer, HttpRestConsumer>();
                builder.Services.AddHostedService<HttpRestConsumer>();
            }

            builder.Services.AddHostedService(provider =>
            {
                return new LemmyNannyOperator(provider.GetRequiredService<IHistoryManager>(),
                    provider.GetRequiredService<IImagesManager>(),
                    provider.GetRequiredService<IOllamaManager>(),
                    provider.GetRequiredService<IWebhooksManager>(),
                    provider.GetRequiredService<ILemmyManager>(),
                    new StartUpStats
                    {
                        LemmyHost = builder.Configuration["LemmyHost"]!,
                        ListingType = listingType,
                        SortType = sortType,
                        Model = model!,
                        Prompt = builder.Configuration["Prompt"]!,
                        StartTime = DateTime.UtcNow,
                        ReadingMode = builder.Configuration.GetValue<bool>("ReadingMode")
                    });
            });
                //builder.Services.AddHostedService(provider =>
                //    new LemmyNannyWorker(provider.GetRequiredService<IHistoryManager>(), 
                //    provider.GetRequiredService<IImagesManager>(),
                //    provider.GetRequiredService<IOllamaManager>(),
                //    provider.GetRequiredService<ILemmyManager>(),
                //    provider.GetRequiredService<IWebhooksManager>(),
                //    new StartUpStats
                //    {
                //        LemmyHost = builder.Configuration["LemmyHost"]!,
                //         ListingType = listingType,
                //         SortType = sortType,
                //         Model = model!,
                //         Prompt = builder.Configuration["Prompt"]!,
                //         StartTime = DateTime.UtcNow,
                //         ReadingMode = builder.Configuration.GetValue<bool>("ReadingMode")
                //    }));

                var dbName = builder.Configuration["SqliteDb"] // defaults to SqliteDb value, if not present makes a new db
                    ?? $"LemmyNanny-{sortType}-{listingType}-{DateTime.Now.ToString("yyyy-MM-dd hh-mm")}.db";
            
            builder.Services.AddHttpClient(OllamaManager.CLIENT_NAME,
                client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration["OllamaUrl"] ?? throw new Exception("OllamaUrl not set"));
                    client.Timeout = TimeSpan.FromMinutes(10);
                });
            builder.Services.AddHttpClient(ImagesManager.CLIENT_NAME);
            builder.Services.AddSingleton<IImagesManager, ImagesManager>(pro=> new ImagesManager(pro.GetRequiredService<IHttpClientFactory>()));
            builder.Services.AddHttpClient(WebhooksManager.CLIENT_NAME);
            builder.Services.AddSingleton<IWebhooksManager>(pro=> 
            new WebhooksManager(pro.GetRequiredService<IHttpClientFactory>(), 
                urls: builder.Configuration.GetRequiredSection("Webhooks").Get<List<WebhookConfig>>()!, 
                DateTime.UtcNow,
                builder.Configuration.GetValue<bool>("ReadingMode")));
            builder.Services.AddSingleton<ILemmyManager>(provider => new LemmyManager(provider.GetRequiredService<ILemmyHttpClient>(), Enum.Parse<SortType>(sortType), Enum.Parse<ListingType>(listingType)));
            builder.Services.AddSingleton<IOllamaApiClient>(pro =>
            {
                var http = pro.GetRequiredService<IHttpClientFactory>();
                var client = new OllamaApiClient(http.CreateClient(OllamaManager.CLIENT_NAME), model ?? throw new Exception("OllamaModel not set"));
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
            builder.Services.AddSingleton<IHistoryManager>(o=> new HistoryManager(dbName, builder.Configuration.GetValue<bool>("WriteDb")));


            IHost host = builder.Build();
            var imageBytes = new[] { File.ReadAllBytes("LemmyNannyLogo.png") };
            
            foreach (var consoleImage in imageBytes.Select(bytes => new CanvasImage(bytes)))
            {
                consoleImage.MaxWidth = 60;
                AnsiConsole.Write(consoleImage);
            }
            AnsiConsole.WriteLine("Welcome to LemmyNanny!");

            host.Run();
            //new WebSocketConsumer().ConnectAndReceive().Wait();
        }
    }
}
