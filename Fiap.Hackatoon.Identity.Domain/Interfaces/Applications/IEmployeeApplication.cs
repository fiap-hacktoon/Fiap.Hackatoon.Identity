﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Applications
{
    public interface IEmployeeApplication
    {
        Task<string> Login(string email, string password);
    }
}
