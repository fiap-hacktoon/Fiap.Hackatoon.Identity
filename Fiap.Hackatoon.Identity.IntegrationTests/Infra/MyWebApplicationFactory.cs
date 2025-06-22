using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fiap.Hackatoon.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Fiap.Hackatoon.Identity.IntegrationTests.Infra
{
    public class MyWebApplicationFactory<T, TContext> : IClassFixture<WebApplicationFactory<T>> where T : class where TContext : DbContext
    {
        protected readonly WebApplicationFactory<T> Factory;
        protected IServiceCollection _servicesCollection { get; set; }

        public MyWebApplicationFactory(WebApplicationFactory<T> factory)
        {
            Factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdentityContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.CreateSQLLite();

                    _servicesCollection = services;

                    var sp = services.BuildServiceProvider();

                    using (var scope = sp.CreateScope())
                    {
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<IdentityContext>();
                        CreateContext(db);
                    }

                });
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
