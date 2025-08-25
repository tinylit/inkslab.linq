using System;
using Inkslab.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Combination.Tests
{
    public class SqlServerConnectionStrings : IConnectionStrings
    {
        public string Strings { get; } = "Server=sqlserver.local.com,1435;database=HysEMall_Promotion;uid=sa;pwd=yyy@123*;MultipleActiveResultSets=True;TrustServerCertificate=true";
    }

    public class Startup : XunitPlus.Startup
    {
        public Startup(Type serviceType) : base(serviceType)
        {
        }

        public override void ConfigureServices(IServiceCollection services, HostBuilderContext context)
        {
            services.UseMySql()
                .UseLinq("server=mysql.local.com;uid=root;pwd=yyy@123*;database=framework;AllowLoadLocalInfile=true");

            services.UseSqlServer()
                .UseDatabase<SqlServerConnectionStrings>();

            services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

            base.ConfigureServices(services, context);
        }
    }
}