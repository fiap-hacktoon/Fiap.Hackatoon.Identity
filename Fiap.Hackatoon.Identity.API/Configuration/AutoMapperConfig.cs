using AutoMapper;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Shared.Dto;

namespace Fiap.Hackatoon.Identity.API.Configuration
{
    public class AutoMapperConfig:Profile
    {
        public AutoMapperConfig()
        {
            CreateMap<ClientCreateDto, Client>().ReverseMap();
            CreateMap<ClientDto, Client>().ReverseMap();

            CreateMap<EmployeeCreateDto, Employee>().ReverseMap();
            CreateMap<EmployeeDto, Employee>().ReverseMap();

        }
    }
}

