using AutoMapper;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Shared.Dto;
using MassTransit;
using Microsoft.Extensions.Options;




namespace Fiap.Hackatoon.Identity.Application.Applications
{
    public class ClientApplication : IClientApplication
    {

        private readonly IClientService _clientService;
        private readonly ITokenApplication _tokenApplication;        
        private readonly IBus _bus;        
        private readonly RabbitMqConnection _rabbitMqConnection;
        private readonly IMapper _mapper;

        public ClientApplication(IClientService clientService, ITokenApplication tokenApplication, IBus bus, IOptions<RabbitMqConnection> rabbitMqOptions, IMapper mapper)
        {
            _clientService = clientService;
            _tokenApplication = tokenApplication;
            _bus = bus;
            _rabbitMqConnection = rabbitMqOptions.Value;
            _mapper = mapper;
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

            var message = _mapper.Map<ClientCreateEvent>(ClientDto);

            var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{_rabbitMqConnection.QueueNameClienteCreate}"));     
            await endpoint.Send(message);          

            return true;
        }

        public async Task<bool> UpdateClient(int clienteId, ClientUpdateDto clientUpdateDto)
        {
            var clienteUpdate = await _clientService.GetClientById(clienteId);            

            if (clienteUpdate is null) throw new Exception($"Client com id:{clienteId} não encontrado");

            if (clienteUpdate.Email != clientUpdateDto.Email) 
            {

                var exist = await _clientService.GetClientByEmail(clientUpdateDto.Email);

                if(exist != null && exist.Id != clienteId)
                    throw new Exception($"O email {clientUpdateDto.Email} já está sendo usado para outro cliente");
            }

            if(clientUpdateDto.Document != clientUpdateDto.Document)
            {
                var exist = await _clientService.GetClientByDocument(clientUpdateDto.Document);

                if (exist != null && exist.Id != clienteId)
                    throw new Exception($"O documento {clientUpdateDto.Email} já está sendo usado para outro cliente");
            }


            var message = _mapper.Map<ClientUpdateEvent>(clientUpdateDto);
            message.Id = clienteId;

            var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{_rabbitMqConnection.QueueNameClienteUpdate}"));

            await endpoint.Send(message);         

            return true;
        }
    }
}

