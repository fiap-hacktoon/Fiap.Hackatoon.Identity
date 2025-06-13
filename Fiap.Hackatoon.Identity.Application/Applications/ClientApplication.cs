using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;



namespace Fiap.Hackatoon.Identity.Application.Applications
{
    public class ClientApplication : IClientApplication
    {

        private readonly IClientService _clientService;
        private readonly ITokenApplication _tokenApplication;

        public ClientApplication(IClientService clientService, ITokenApplication tokenApplication)
        {
            _clientService = clientService;
            _tokenApplication = tokenApplication;
        }


        public async Task<string> Login(string search, string password)
        {
            try
            {
                var user = await _clientService.GetClient(search, password);

                if (user is null)
                    throw new Exception("Usuário ou senha invalido");

                var token = _tokenApplication.GenerateToken(user);

                return token;

            }
            catch (Exception)
            {

                throw;
            }
        }


        public async Task<bool> AddClient(ClientDto ClientDto)
        {

            
            return true;
        }
    }
}
