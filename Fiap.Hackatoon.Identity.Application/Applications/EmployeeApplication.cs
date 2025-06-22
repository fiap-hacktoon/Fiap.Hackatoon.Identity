using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Services;
using MassTransit;


namespace Fiap.Hackatoon.Identity.Application.Applications
{
    public class EmployeeApplication : IEmployeeApplication
    {
        private readonly IEmployeeService _employeeService;
        private readonly ITokenApplication _tokenApplication;
        private readonly IBus _bus;

        public EmployeeApplication(IEmployeeService employeeService, IBus bus, ITokenApplication tokenApplication)
        {
            _employeeService = employeeService;
            _bus = bus;
            _tokenApplication = tokenApplication;
        }

        public async Task<string> Login(string email, string password)
        {
            try
            {
                var user = await _employeeService.GetEmployeeLogin(email, password);

                if (user is null)
                    return null;

                var token = _tokenApplication.GenerateToken(user);

                return token;

            }
            catch (Exception)
            {

                throw;
            }
        }


        public async Task<bool> AddEmployee(EmployeeCreateDto employeeCreate)
        {

            var employee = await _employeeService.GetEmployeeByEmail(employeeCreate.Email);

            if (employee is not null) throw new Exception("O email já existe cadastrado");

            await _bus.Publish(employeeCreate);

            return true;            
        }
    }
}
