using Fiap.Hackatoon.Identity.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.IntegrationTests.Infra
{
   public static class CreateDBSqlTest
    {
        public static void CreateSQLLite(this IServiceCollection services)
        {
            var databaseName = Guid.NewGuid().ToString();
            var connectionString = $"Data source={databaseName};Mode=Memory;Cache=Shared";
            var connection = new SqliteConnection(connectionString);

            connection.Open();

            services.AddDbContext<IdentityContext>(options =>
            options.UseSqlite(connection));

        }
    }
}
