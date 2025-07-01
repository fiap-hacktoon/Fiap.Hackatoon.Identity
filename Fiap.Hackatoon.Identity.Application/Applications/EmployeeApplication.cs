using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Options;


namespace Fiap.Hackatoon.Identity.Application.Applications
{
    public class EmployeeApplication : IEmployeeApplication
    {
        private readonly IEmployeeService _employeeService;
        private readonly ITokenApplication _tokenApplication;
        private readonly IBusService _bus;
        private readonly RabbitMqConnection _rabbitMqConnection;

        public EmployeeApplication(IEmployeeService employeeService, IBusService bus, ITokenApplication tokenApplication, IOptions<RabbitMqConnection> rabbitMqOptions)
        {
            _employeeService = employeeService;
            _bus = bus;
            _tokenApplication = tokenApplication;
            _rabbitMqConnection = rabbitMqOptions.Value;
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

            await _bus.SendToBus(employeeCreate, _rabbitMqConnection.QueueNameEmployeeCreate);

            return true;            
        }

        public async Task<bool> UpdateEmployee(int employeeId, EmployeeUpdateDto employeeUpdateDto)
        {
            var employeeUpdate =  await _employeeService.GetEmployeeById(employeeId);


            if (employeeUpdate is null) throw new Exception($"Employee com  id:{employeeId} não encontrado");

            if (employeeUpdate.Email != employeeUpdateDto.Email)
            {
                if (await _employeeService.GetEmployeeByEmail(employeeUpdateDto.Email) != null) throw new Exception($"O email {employeeUpdateDto.Email} já está sendo usando para outro employee");
            }

            await _bus.SendToBus(employeeUpdateDto, _rabbitMqConnection.QueueNameEmployeeUpdate);

            return true;
        }      
    }
}
