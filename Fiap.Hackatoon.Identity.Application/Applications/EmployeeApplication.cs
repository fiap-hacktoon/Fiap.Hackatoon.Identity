using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Application.Applications
{
    public class EmployeeApplication : IEmployeeApplication
    {
        private readonly IEmployeeService _employeeService;
        private readonly ITokenApplication _tokenApplication;

        public EmployeeApplication(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
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
    }
}
