using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace SqlServer.Tests
{
    public class Startup : XunitPlus.Startup
    {
        public Startup(Type serviceType) : base(serviceType)
        {
        }

        public override void ConfigureServices(IServiceCollection services, HostBuilderContext context)
        {
            services.UseSqlServer()
                .UseLinq("Server=sqlserver.local.com,1435;database=HysEMall_Promotion;uid=sa;pwd=yyy@123*;MultipleActiveResultSets=True;TrustServerCertificate=true");

            services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

            base.ConfigureServices(services, context);
        }
    }
}