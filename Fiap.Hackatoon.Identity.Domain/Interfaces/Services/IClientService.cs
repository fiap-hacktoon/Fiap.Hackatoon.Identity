using Fiap.Hackatoon.Identity.Domain.Entities;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Services
{
    public interface IClientService
    {
        Task<Client?> GetClientLogin(string search, string password);
        Task<Client?> GetClientByEmailOrDocument(string email, string document);
        Task<Client?> GetClientById(int id);
        Task<Client?> GetClientByEmail(string email);
    }
}
