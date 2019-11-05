using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Autodesk.Forge.Core;
using Autodesk.Forge.DesignAutomation;

namespace forgeSample.Controllers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).ConfigureAppConfiguration(builder =>
            {
                builder.AddForgeAlternativeEnvironmentVariables();
            }).ConfigureServices((hostContext, services) =>
            {
                services.AddDesignAutomation(hostContext.Configuration);
            }).Build().Run();
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
