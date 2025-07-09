using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Elastic;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;

namespace Fiap.Hackatoon.Identity.Domain.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;
        private readonly IElasticClient<Client> _elasticClient;

        public ClientService(IClientRepository clientRepository, IElasticClient<Client> elasticClient)
        {
            _clientRepository = clientRepository;
            _elasticClient = elasticClient;
        }

        public async Task<Client?> GetClientLogin(string search, string password)
        {
            var client = await _clientRepository
                .FindOne(x => (x.Email == search | x.Document == search)
                       && x.Password == password);

            if (client is null) return null;
            return client;
        }

        public async Task<Client?> GetClientByEmailOrDocument(string email, string document)
        {
            var client = await _clientRepository
               .FindOne(x => (x.Email == email | x.Document == document));

            if (client is null) return null;
            return client;
        }

        public async Task<Client?> GetClientByEmail(string email)
        {
            var client = await _clientRepository
               .FindOne(x => x.Email == email);

            if (client is null) return null;
            return client;
        }

        public async Task<Client?> GetClientByDocument(string document)
        {
            var client = await _clientRepository
               .FindOne(x => x.Document == document);

            if (client is null) return null;
            return client;
        }

        public async Task<Client?> GetClientById(int id)
        {
            var resultElastic = await _elasticClient.Search("client", q => q.Bool(b => b
                            .Must(
                                mu => mu.Match(m => m.Field(f => f.Id).Query(id))
                            )
                        ), 0, 1);


            if (resultElastic.Any()) return resultElastic.FirstOrDefault();

            return await _clientRepository.FindOne(x => x.Id == id);
        }
    }
}
