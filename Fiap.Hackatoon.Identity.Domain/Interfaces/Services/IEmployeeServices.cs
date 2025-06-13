using Fiap.Hackatoon.Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<Employee?> GetEmployee(string email, string password);
    }
}
