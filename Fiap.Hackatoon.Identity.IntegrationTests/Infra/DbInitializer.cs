using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using Fiap.Hackatoon.Identity.Infrastructure.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.IntegrationTests.Infra
{
   public class DbInitializer
    {
        public static void InitializerSqlite(IdentityContext context)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            Seed(context);
        }

        private static void Seed(IdentityContext context)
        {           
            context.Clients.RemoveRange(context.Clients);
            context.Employees.RemoveRange(context.Employees);            
            context.SaveChanges();

            // Adiciona um cliente para o teste de Login
            context.Clients.Add(new Client
            {
                Id = 1,
                Name = "Cliente Login Teste",
                Email = "login@cliente.com",
                Document = "11122233344",
                Password = "Password123", // Importante: Se você usa hash na vida real, aqui também precisa ser hash!
                TypeRole = TypeRole.Client,
                Birth = new DateTime(1980, 1, 1),
                Creation = DateTime.UtcNow
            });

            // Adiciona outro cliente para o teste de Login (documento e senha)
            context.Clients.Add(new Client
            {
                Id = 2,
                Name = "Cliente Doc Teste",
                Email = "doc@cliente.com",
                Document = "55566677788",
                Password = "Password123",
                TypeRole = TypeRole.Client,
                Birth = new DateTime(1985, 5, 5),
                Creation = DateTime.UtcNow
            });

            // Adiciona um cliente para o teste de GetClientById
            context.Clients.Add(new Client
            {
                Id = 3,
                Name = "Cliente ID Teste",
                Email = "id@cliente.com",
                Document = "99988877766",
                Password = "Password123",
                TypeRole = TypeRole.Client,
                Birth = new DateTime(1990, 10, 10),
                Creation = DateTime.UtcNow
            });

            // Adiciona um cliente para o teste de email/documento existente na criação
            context.Clients.Add(new Client
            {
                Id = 4,
                Name = "Cliente Existente",
                Email = "existing@cliente.com",
                Document = "00011122233",
                Password = "Password123",
                TypeRole = TypeRole.Client,
                Birth = new DateTime(1975, 2, 2),
                Creation = DateTime.UtcNow
            });


            context.Employees.Add(new Employee
            {
                Id = 101,
                Name = "Teste Funcionario",
                Email = "test@employee.com",
                Password = "EmployeePassword123",
                TypeRole = TypeRole.Manager,
                Creation = DateTime.UtcNow
            });



            context.SaveChanges();
        }

        private static string GetJson(string fileName)
        {
            return File.ReadAllText($"./Seeds/{fileName}");
        }
    }
}
