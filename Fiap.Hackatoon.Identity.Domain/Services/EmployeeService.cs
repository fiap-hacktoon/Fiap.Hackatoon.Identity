using Elastic.Clients.Elasticsearch;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Elastic;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Repositories;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;

namespace Fiap.Hackatoon.Identity.Domain.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IElasticClient<Employee> _elasticClient;

        public EmployeeService(IEmployeeRepository employeeRepository, IElasticClient<Employee> elasticClient)
        {
            _employeeRepository = employeeRepository;
            _elasticClient = elasticClient;
        }

        public async Task<Employee?> GetEmployeeLogin(string email, string password)
        {
            var employee = await _employeeRepository
               .FindOne(x => (x.Email == email)
                      && x.Password == password);

            if (employee is null) return null;
            return employee;
        }

        public async Task<Employee?> GetEmployeeByEmail(string email)
        {

            var resultElastic = await _elasticClient.Search("employee", 
                            q => q.Bool(b => b
                            .Must(
                               mu => mu.Match(m => m.Field(f => f.Email).Query(email))
                            )
                       ), 0, 1);


            if (resultElastic.Any()) return resultElastic.FirstOrDefault();

            return await _employeeRepository
              .FindOne(x => (x.Email == email));
        }

        public async Task<Employee?> GetEmployeeById(int id)
        {

            var resultElastic = await _elasticClient.Search("employee", q => q.Bool(b => b
                           .Must(
                               mu => mu.Match(m => m.Field(f => f.Id).Query(id))
                           )
                       ), 0, 1);


            if (resultElastic.Any()) return resultElastic.FirstOrDefault();

            return await _employeeRepository.FindOne(x => x.Id == id);

        }
    }
}
