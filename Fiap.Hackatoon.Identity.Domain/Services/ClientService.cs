﻿using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;

namespace Fiap.Hackatoon.Identity.Domain.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;

        public ClientService(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<Client?> GetClient(string search, string password)
        {
            var client = await _clientRepository
                .FindOne(x => (x.Email == search | x.Document == search)
                       && x.Password == password);

            if (client is null) return null;
            return client;
        }       
    }
}
