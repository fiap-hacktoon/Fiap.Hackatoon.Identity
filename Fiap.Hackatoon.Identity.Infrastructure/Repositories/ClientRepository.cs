using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        public Task<Client> GetClientByDocument(string document)
        {
            throw new NotImplementedException();
        }

        public Task<Client> GetClientByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public Task<Client> GetClientByEmailOrDocument(string search)
        {
            throw new NotImplementedException();
        }

        public Task<Client> GetClientById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Client>> GetClients()
        {
            throw new NotImplementedException();
        }
    }
}
