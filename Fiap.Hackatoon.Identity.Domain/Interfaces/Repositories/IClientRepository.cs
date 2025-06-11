using Fiap.Hackatoon.Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories
{
    public interface IClientRepository
    {
        Task<Client> GetClientById(int id);

        Task<Client> GetClientByEmail(string email);

        Task<Client> GetClientByDocument(string document);

        Task<Client> GetClientByEmailOrDocument(string search);

        Task<IEnumerable<Client>> GetClients();
    }
}
