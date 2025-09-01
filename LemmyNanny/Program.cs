using dotNETLemmy.API;
using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OllamaSharp;

namespace LemmyNanny
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddHostedService(provider =>
                new LemmyWorker(provider.GetRequiredService<ILemmyHttpClient>(), 
                provider.GetRequiredService<HistoryManager>(), 
                provider.GetRequiredService<IHttpClientFactory>(),
                provider.GetRequiredService<IOllamaApiClient>(),
                builder.Configuration["Prompt"]?? throw new Exception("Prompt not set"),
                Enum.Parse<SortType>(builder.Configuration["SortType"]!),
                Enum.Parse<ListingType>(builder.Configuration["ListingType"]!)));

            builder.Services.AddHttpClient("OllamaClient",
                client =>
                {
                    client.BaseAddress = new Uri(builder.Configuration["OllamaUrl"] ?? throw new Exception("OllamaUrl not set"));
                    client.Timeout = TimeSpan.FromMinutes(10);
                });
            builder.Services.AddHttpClient("PictrsClient");

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
            builder.Services.AddSingleton(o=> new HistoryManager(builder.Configuration["SqliteDb"] ?? throw new Exception("SqliteDb not set")));


            IHost host = builder.Build();

            host.Run();
        }
    }
}
