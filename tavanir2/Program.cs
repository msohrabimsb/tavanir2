using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace tavanir2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        // Set properties and call methods on options
                        serverOptions.Limits.MaxRequestBodySize = 1073741824; // 1 GB
                    })
                    .UseStartup<Startup>();
                });
    }
}
