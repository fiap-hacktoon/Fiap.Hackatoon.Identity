using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;

namespace Fiap.Hackatoon.Identity.Domain.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeService(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<Employee?> GetEmployee(string email, string password)
        {
            var employee = await _employeeRepository
               .FindOne(x => (x.Email == email)
                      && x.Password == password);

            if (employee is null) return null;
            return employee;
        }
    }
}
