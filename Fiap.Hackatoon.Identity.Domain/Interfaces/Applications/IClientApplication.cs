﻿using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
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
        Task<bool> AddClient(ClientDto ClientDto);
    }
}
