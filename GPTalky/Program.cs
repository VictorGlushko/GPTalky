using Entity;
using Entity.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Core;
using CodeImageGenerator;

namespace GPTalky
{
    internal class Program
    {
        static void Main(string[] args) 
        {
            var builder = new ConfigurationBuilder();
            BuildConfig(builder);
            IConfiguration config = builder.Build();


            IHost host = CreateHostBuilder(config).Build();
            var scope = host.Services.CreateScope();

            var services = scope.ServiceProvider;


            try
            {
                var context = services.GetRequiredService<GpTalkDbContext>();
                context.Database.EnsureCreated();

                services.GetRequiredService<Worker>().Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();

        }

        private static IHostBuilder CreateHostBuilder(IConfiguration builder)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<Worker>();
                    services.AddSingleton<GpTalkDbContext>();
                    services.AddSingleton<Commander>();
                    services.AddSingleton<Generator>();
                    services.AddSingleton<IUnitOfWork,UnitOfWork>();

                
                    

                    //services.AddSingleton(new TelegramBotClient(context.Configuration.GetValue<string>("TelegramApiKey")!));
                    //services.AddEntityFrameworkNpgsql();
                });
        }

        static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
        }

    }

}