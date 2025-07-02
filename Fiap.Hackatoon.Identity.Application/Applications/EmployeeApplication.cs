using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Services;
using Fiap.Hackatoon.Shared.Dto;
using MassTransit;
using Microsoft.Extensions.Options;


namespace Fiap.Hackatoon.Identity.Application.Applications
{
    public class EmployeeApplication : IEmployeeApplication
    {
        private readonly IEmployeeService _employeeService;
        private readonly ITokenApplication _tokenApplication;
        private readonly IBus _bus;
        private readonly RabbitMqConnection _rabbitMqConnection;

        public EmployeeApplication(IEmployeeService employeeService, IBus bus, ITokenApplication tokenApplication, IOptions<RabbitMqConnection> rabbitMqOptions)
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

            var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{_rabbitMqConnection.QueueNameEmployeeCreate}"));

            await endpoint.Send(employeeCreate);            

            return true;            
        }

        public async Task<bool> UpdateEmployee(int employeeId, EmployeeUpdateDto employeeUpdateDto)
        {
            var employeeUpdate =  await _employeeService.GetEmployeeById(employeeId);


            if (employeeUpdate is null) throw new Exception($"Employee com id:{employeeId} não encontrado");

            if (employeeUpdate.Email != employeeUpdateDto.Email)
            {
                if (await _employeeService.GetEmployeeByEmail(employeeUpdateDto.Email) != null) throw new Exception($"O email {employeeUpdateDto.Email} já está sendo usado para outro employee");
            }

            var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{_rabbitMqConnection.QueueNameEmployeeUpdate}"));

            await endpoint.Send(employeeUpdateDto);            

            return true;
        }      
    }
}
