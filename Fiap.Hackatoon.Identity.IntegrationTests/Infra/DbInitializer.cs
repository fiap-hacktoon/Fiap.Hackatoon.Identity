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
            //var contatosJson = GetJson("contatos.json");

            //List<Contatos> contatos = JsonConvert.DeserializeObject<List<Contatos>>(contatosJson);
            //context.Contatos.AddRange(contatos);


            context.SaveChanges();
        }

        private static string GetJson(string fileName)
        {
            return File.ReadAllText($"./Seeds/{fileName}");
        }
    }
}
