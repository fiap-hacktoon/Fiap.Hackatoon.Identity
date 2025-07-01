using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;



namespace Fiap.Hackatoon.Identity.Application.Applications
{
    public class ClientApplication : IClientApplication
    {

        private readonly IClientService _clientService;
        private readonly ITokenApplication _tokenApplication;        
        private readonly IBusService _bus;        
        private readonly RabbitMqConnection _rabbitMqConnection;
        private readonly IUserService _userService;

        public ClientApplication(IClientService clientService, ITokenApplication tokenApplication, IBusService bus, IOptions<RabbitMqConnection> rabbitMqOptions, IUserService userService)
        {
            _clientService = clientService;
            _tokenApplication = tokenApplication;
            _bus = bus;
            _rabbitMqConnection = rabbitMqOptions.Value;
            _userService = userService;
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

            await _bus.SendToBus(ClientDto, _rabbitMqConnection.QueueNameClienteCreate);

            return true;
        }

        public async Task<bool> UpdateClient(int clienteId, ClientUpdateDto clientUpdateDto)
        {
            var clienteUpdate = await _clientService.GetClientById(clienteId);            

            if (clienteUpdate is null) throw new Exception($"client com  id:{clienteId} não encontrado");

            if (clienteUpdate.Email != clientUpdateDto.Email || clientUpdateDto.Document != clientUpdateDto.Document)
            {

                var exist = await _clientService.GetClientByEmailOrDocument(clientUpdateDto.Email, clientUpdateDto.Document);

                if(exist != null && exist.Id != clienteId)
                    throw new Exception($"O email {clientUpdateDto.Email} já está sendo usando para outro cliente");
            }

            await _bus.SendToBus(clientUpdateDto, _rabbitMqConnection.QueueNameClienteUpdate);

            return true;
        }
    }
}

