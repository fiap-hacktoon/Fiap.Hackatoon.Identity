using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.Hackatoon.Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmployeeApplication _employeeApplication;

        public EmployeeController(ILogger<EmployeeController> logger, IEmployeeApplication employeeApplication)
        {
            _logger = logger;
            _employeeApplication = employeeApplication;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] EmployeeLoginDto employeeLoginDto)
        {
            _logger.LogInformation($"Start Login Employee: {employeeLoginDto.Email}");
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                _logger.LogInformation($"Login: {employeeLoginDto.Email}");
                var token = await _employeeApplication.Login(employeeLoginDto.Email, employeeLoginDto.Password);

                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Login Employee failed: {employeeLoginDto.Email}. Error: ${ex.Message ?? ""}");
                return BadRequest("Erro ao tentar efeutar o login");
            }
        }
    }
}
