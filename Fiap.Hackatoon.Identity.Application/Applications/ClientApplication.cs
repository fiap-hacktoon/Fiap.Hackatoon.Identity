using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using MassTransit;



namespace Fiap.Hackatoon.Identity.Application.Applications
{
    public class ClientApplication : IClientApplication
    {

        private readonly IClientService _clientService;
        private readonly ITokenApplication _tokenApplication;
        private readonly IBus _bus;

        public ClientApplication(IClientService clientService, ITokenApplication tokenApplication, IBus bus)
        {
            _clientService = clientService;
            _tokenApplication = tokenApplication;
            _bus = bus;
        }


        public async Task<string> Login(string search, string password)
        {
            try
            {
                var user = await _clientService.GetClientLogin(search, password);

                if (user is null)
                    return null;
             
                var token = _tokenApplication.GenerateToken(user);

                return token;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<bool> AddClient(ClientCreateDto ClientDto)
        {
            var client = await _clientService.GetClientByEmailOrDocument(ClientDto.Email, ClientDto.Document);

            if (client is not null) throw new Exception("O email/document já existe cadastrado");
            
            await _bus.Publish(ClientDto);

            return true;
        }
    }
}

