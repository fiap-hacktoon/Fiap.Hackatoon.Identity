using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        public Task<Employee> GetEmployeeByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public Task<Employee> GetEmployeeById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Employee>> GetEmployees()
        {
            throw new NotImplementedException();
        }
    }
}
