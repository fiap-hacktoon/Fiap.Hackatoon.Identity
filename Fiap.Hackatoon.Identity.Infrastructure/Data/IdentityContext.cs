using Fiap.Hackatoon.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Infrastructure.Data
{
    public class IdentityContext: DbContext
    {
        public DbSet<Client> Clients { get; set; }
        public DbSet<Employee> Employees { get; set; }


        public IdentityContext(DbContextOptions<IdentityContext> options):base(options)
        {
                
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(
               e => e.GetProperties().Where(p => p.ClrType == typeof(string))))
                property.SetColumnType("varchar(100)");


            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityContext).Assembly);
        }


    }
}
