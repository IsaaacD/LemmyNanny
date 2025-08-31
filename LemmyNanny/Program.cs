using dotNETLemmy.API;
using dotNETLemmy.API.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LemmyNanny
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddHostedService(provider =>
                new LemmyWorker(provider.GetRequiredService<ILemmyHttpClient>(), provider.GetRequiredService<HistoryManager>())
                {
                    BaseAddress = builder.Configuration["LemmyHost"] ?? throw new Exception("LemmyHost not set"),
                    LemmyUserName = builder.Configuration["LemmyUserName"] ?? throw new Exception("LemmyUserName not set"),
                    LemmyPassword = builder.Configuration["LemmyPassword"] ?? throw new Exception("LemmyPassword not set"),    
                    OllamaModel = builder.Configuration["OllamaModel"] ?? throw new Exception("OllamaModel not set"),
                    OllamaUrl = builder.Configuration["OllamaUrl"] ?? throw new Exception("OllamaUrl not set")

                });

            builder.Services.AddHttpClient<ILemmyHttpClient, LemmyHttpClient>();
            builder.Services.AddSingleton(o=> new HistoryManager(builder.Configuration["SqliteDb"] ?? throw new Exception("SqliteDb not set")));
            IHost host = builder.Build();

            host.Run();
        }
    }
}
