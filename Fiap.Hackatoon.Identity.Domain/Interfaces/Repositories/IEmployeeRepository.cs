using Fiap.Hackatoon.Identity.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories
{
    public interface IEmployeeRepository
    {
        Task<Employee> GetEmployeeById(int id);

        Task<Employee> GetEmployeeByEmail(string email);

        Task<IEnumerable<Employee>> GetEmployees();
    }
}
