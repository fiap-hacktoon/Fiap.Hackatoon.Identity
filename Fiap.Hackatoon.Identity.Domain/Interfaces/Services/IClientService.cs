using Fiap.Hackatoon.Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Services
{
    public interface IClientService
    {
        Task<Client?> GetClientLogin(string search, string password);
        Task<Client?> GetClientByEmailOrDocument(string email, string document);
        Task<Client?> GetClientById(int id);
    }
}
