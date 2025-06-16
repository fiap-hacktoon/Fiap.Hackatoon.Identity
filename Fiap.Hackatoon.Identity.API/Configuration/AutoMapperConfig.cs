using AutoMapper;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;

namespace Fiap.Hackatoon.Identity.API.Configuration
{
    public class AutoMapperConfig:Profile
    {
        public AutoMapperConfig()
        {
            CreateMap<ClientDto, Client>().ReverseMap();
        }
    }
}
