using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace PostgreSQL.Tests
{    public class Startup : XunitPlus.Startup
    {
        public Startup(Type serviceType) : base(serviceType)
        {
        }

        public override void ConfigureServices(IServiceCollection services, HostBuilderContext context)
        {
            services.UsePostgreSQL()
                .UseLinq("Host=npgsql.local.com;Database=framework;Username=root;Password=pgsql@123");

            services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

            base.ConfigureServices(services, context);
        }
    }
}