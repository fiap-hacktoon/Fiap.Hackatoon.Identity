using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Infrastructure.Data;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Fiap.Hackatoon.Identity.IntegrationTests.Infra
{
    public class MyWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        public ITestHarness BusTestHarness { get; private set; }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTesting");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdentityContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.CreateSQLLite();

                services.AddMassTransitTestHarness(x =>
                {
                    x.AddConsumer<DummyConsumer>();
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context);
                        cfg.ReceiveEndpoint("test-endpoint", e => { });
                    });
                });


                var sp = services.BuildServiceProvider();

                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<IdentityContext>();

                    BusTestHarness = scopedServices.GetRequiredService<ITestHarness>();                                        

                    CreateContext(db);
                }

            });
        }        

        protected void CreateContext(IdentityContext context)
        {
            try
            {
                DbInitializer.InitializerSqlite(context);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
