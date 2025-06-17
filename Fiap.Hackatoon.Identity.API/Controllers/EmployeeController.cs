using AutoMapper;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Fiap.Hackatoon.Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmployeeApplication _employeeApplication;
        private readonly IEmployeeService _employeeService;
        private readonly IMapper _mapper;

        public EmployeeController(ILogger<EmployeeController> logger, IEmployeeApplication employeeApplication, IEmployeeService employeeService, IMapper mapper)
        {
            _logger = logger;
            _employeeApplication = employeeApplication;
            _employeeService = employeeService;
            _mapper = mapper;
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

                if (string.IsNullOrEmpty(token))
                    return Unauthorized();

                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Login Employee failed: {employeeLoginDto.Email}. Error: ${ex.Message ?? ""}");
                return BadRequest("Erro ao tentar efeutar o login");
            }
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] EmployeeCreateDto employeeCreateDto)
        {
            _logger.LogInformation($"Start add employee: {employeeCreateDto.Email}");
            try
            {
                //var employee = _employeeService.GetEmployeeById()
                return StatusCode(201);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Add employee failed:. Error: ${ex.Message ?? ""}");
                return BadRequest("Erro ao tentar efeutar o login");                
            }
        }

        [HttpGet("GetEmployeeById/{id:int}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {

            _logger.LogInformation($"Start GetEmployeeById: {id}");
            try
            {
                var client = await _employeeService.GetEmployeeById(id);

                if (client is not null)
                    return Ok(_mapper.Map<EmployeeDto>(client));
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error GetEmployeeById ${ex.Message ?? ""}");
                throw;
            }

        }
    }
    
}
