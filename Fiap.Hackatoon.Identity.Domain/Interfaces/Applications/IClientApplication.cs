using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Applications
{
    public interface IClientApplication
    {
        Task<string> Login(string search, string password);
        Task<bool> AddClient(ClientCreateDto ClientDto);
        Task<bool> UpdateClient(int clienteId, ClientUpdateDto clientUpdateDto);
    }
}
