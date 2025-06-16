using Fiap.Hackatoon.Identity.Domain.Entities;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Services
{
    public interface IClientService
    {
        Task<Client?> GetClient(string search, string password);        
    }
}
