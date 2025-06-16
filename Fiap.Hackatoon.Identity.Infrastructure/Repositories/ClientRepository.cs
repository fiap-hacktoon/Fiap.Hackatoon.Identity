using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories;
using Fiap.Hackatoon.Identity.Infrastructure.Data;

namespace Fiap.Hackatoon.Identity.Infrastructure.Repositories
{
    public class ClientRepository : Repository<Client>, IClientRepository
    {
        public ClientRepository(IdentityContext identityContext) : base(identityContext)
        {
        }
    }
}
