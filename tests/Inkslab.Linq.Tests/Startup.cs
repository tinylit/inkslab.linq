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
            services.UseMySql()
                .UseLinq("server=mysql.local.com;uid=root;pwd=yyy@123*;database=framework;AllowLoadLocalInfile=true;Charset=utf8mb4;");

            services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

            base.ConfigureServices(services, context);
        }
    }
}