using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Elastic;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Services;
using Fiap.Hackatoon.Identity.Infrastructure.Data;
using Fiap.Hackatoon.Identity.Infrastructure.ElasticSearch;
using Fiap.Hackatoon.Identity.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace Fiap.Hackatoon.Identity.API.Configuration
{
    public static class DependencyInjectionConfig
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            #region Context
            services.AddScoped<IdentityContext>();
            #endregion

            #region Repository
            //repository
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            #endregion

            #region Application
            services.AddScoped<ITokenApplication, TokenApplication>();
            services.AddScoped<IClientApplication, ClientApplication>();
            services.AddScoped<IEmployeeApplication, EmployeeApplication>();
            #endregion

            #region Service
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IEmployeeService, EmployeeService>();            
            services.AddScoped<IUserService, UserService>();
            services.AddSingleton<IElasticSettings>(sp => sp.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddSingleton(typeof(IElasticClient<>), typeof(ElasticClient<>));
            #endregion services           

            services.AddHttpContextAccessor();
        }
    }
}
