using Fiap.Hackatoon.Identity.Domain.Entities;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<Employee?> GetEmployeeLogin(string email, string password);
        Task<Employee?> GetEmployeeById(int id);
        Task<Employee?> GetEmployeeByEmail(string email);
    }
}
