using Fiap.Hackatoon.Identity.Domain.Entities;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<Employee?> GetEmployee(string email, string password);
    }
}
