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

            // Clientes para Login (test@client.com, 55566677788)
            context.Clients.Add(new Client { Id = 1, Name = "Cliente Login Email", Email = "test@client.com", Document = "11122233344", Password = "Password123", TypeRole = TypeRole.Client, Birth = new DateTime(1980, 1, 1), Creation = DateTime.UtcNow });
            context.Clients.Add(new Client { Id = 2, Name = "Cliente Login Doc", Email = "doc@client.com", Document = "55566677788", Password = "Password123", TypeRole = TypeRole.Client, Birth = new DateTime(1985, 5, 5), Creation = DateTime.UtcNow });

            // Cliente para GetClientById
            context.Clients.Add(new Client { Id = 3, Name = "Cliente ID Teste", Email = "id@cliente.com", Document = "99988877766", Password = "Password123", TypeRole = TypeRole.Client, Birth = new DateTime(1990, 10, 10), Creation = DateTime.UtcNow });

            // Cliente para GetClientByEmail
            context.Clients.Add(new Client { Id = 4, Name = "Cliente Email Teste", Email = "emailget@cliente.com", Document = "12121212121", Password = "Password123", TypeRole = TypeRole.Manager, Birth = new DateTime(1992, 11, 11), Creation = DateTime.UtcNow });

            // Cliente para UpdateClient (com ID específico)
            context.Clients.Add(new Client { Id = 5, Name = "Cliente Update Original", Email = "update@cliente.com", Document = "12345678910", Password = "Password123", TypeRole = TypeRole.Client, Birth = new DateTime(1995, 3, 15), Creation = DateTime.UtcNow });
            // Outro cliente com email que poderia ser usado para conflito no Update
            context.Clients.Add(new Client { Id = 6, Name = "Cliente Conflito Email", Email = "conflito@cliente.com", Document = "01010101010", Password = "Password123", TypeRole = TypeRole.Client, Birth = new DateTime(1993, 7, 7), Creation = DateTime.UtcNow });


            // Funcionário para autenticação (Login e GetAuthTokenForEmployee)
            context.Employees.Add(new Employee { Id = 107, Name = "Funcionario Teste", Email = "test@employee.com", Password = "EmployeePassword123", TypeRole = TypeRole.Manager, Creation = DateTime.UtcNow });
            context.Employees.Add(new Employee { Id = 108, Name = "Funcionario Cozinha", Email = "kitchen@employee.com", Password = "EmployeePassword123", TypeRole = TypeRole.Kitchen, Creation = DateTime.UtcNow });

            // --- Dados de Funcionários ---
            // Funcionário para Login e para operar como Manager
            context.Employees.Add(new Employee { Id = 101, Name = "Funcionario Manager", Email = "manager@employee.com", Password = "EmployeePassword123", TypeRole = TypeRole.Manager, Creation = DateTime.UtcNow });
            // Funcionário para login de teste (sem ser manager, se precisar de Forbidden)
            context.Employees.Add(new Employee { Id = 102, Name = "Funcionario Atendente", Email = "attendant@employee.com", Password = "EmployeePassword123", TypeRole = TypeRole.Attendant, Creation = DateTime.UtcNow });
            context.Employees.Add(new Employee { Id = 103, Name = "Funcionario Cozinha", Email = "kitchen@employee.com", Password = "EmployeePassword123", TypeRole = TypeRole.Kitchen, Creation = DateTime.UtcNow });

            // Funcionário para GetEmployeeById e UpdateEmployee
            context.Employees.Add(new Employee { Id = 104, Name = "Funcionario ID Teste", Email = "id@employee.com", Password = "EmployeePassword123", TypeRole = TypeRole.Manager, Creation = DateTime.UtcNow });
            // Funcionário para GetEmployeeByEmail
            context.Employees.Add(new Employee { Id = 105, Name = "Funcionario Email Teste", Email = "emailget@employee.com", Password = "EmployeePassword123", TypeRole = TypeRole.Manager, Creation = DateTime.UtcNow });
            // Funcionário com email para conflito no Update
            context.Employees.Add(new Employee { Id = 106, Name = "Funcionario Conflito", Email = "conflito@employee.com", Password = "EmployeePassword123", TypeRole = TypeRole.Manager, Creation = DateTime.UtcNow });

            context.SaveChanges();
        }

        private static string GetJson(string fileName)
        {
            return File.ReadAllText($"./Seeds/{fileName}");
        }
    }
}
