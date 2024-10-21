using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Inkslab.Linq.Tests
{
    public class Startup : XunitPlus.Startup
    {
        public Startup(Type serviceType) : base(serviceType)
        {
        }

        public override void ConfigureServices(IServiceCollection services, HostBuilderContext context)
        {
            var connectionStrings = Environment.GetEnvironmentVariable("connectionStrings");

            services.UseMySql(connectionStrings ?? "server=mysql.local.com;uid=root;pwd=yyy@123*;database=framework;AllowLoadLocalInfile=true");

            services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

            base.ConfigureServices(services, context);
        }
    }
}