using AutoMapper;
using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Services;
using Fiap.Hackatoon.Shared.Dto;
using Microsoft.AspNetCore.Authorization;
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


        [Authorize(Roles = "Manager")]
        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] EmployeeCreateDto employeeCreateDto)
        {
            _logger.LogInformation($"Start add client: {employeeCreateDto.Email}");
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var addClient = await _employeeApplication.AddEmployee(employeeCreateDto);

                if (addClient)
                    return StatusCode(201);
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Add employss failed:. Error: ${ex.Message ?? ""}");
                return BadRequest($"Erro ao tentar efeutar o cadastro do employee. {ex.Message}");
            }
        }


        [Authorize(Roles = "Manager")]
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
                return BadRequest("Erro ao tentar consultar o employee por id");
            }

        }

        [Authorize(Roles = "Manager")]
        [HttpGet("GetEmployeeByEmail/{email}")]
        public async Task<IActionResult> GetEmployeeByEmail(string email)
        {

            _logger.LogInformation($"Start GetEmployeeByEmail: {email}");
            try
            {
                var client = await _employeeService.GetEmployeeByEmail(email);

                if (client is not null)
                    return Ok(_mapper.Map<EmployeeDto>(client));
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error GetEmployeeByEmail ${ex.Message ?? ""}");
                return BadRequest("Erro ao tentar consultar o employee por id");
            }

        }

        [Authorize(Roles = "Manager,Attendant,Kitchen")]
        [HttpPut("UpdateEmployee/{employeeId:int}")]
        public async Task<IActionResult> UpdateEmployee(int employeeId, [FromBody] EmployeeUpdateDto employeeUpdateDto)
        {
            _logger.LogInformation($"update client: {employeeUpdateDto.Email}");
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var addClient = await _employeeApplication.UpdateEmployee(employeeId, employeeUpdateDto);

                if (addClient)
                    return NoContent();
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Update employee failed:. Error: ${ex.Message ?? ""}");
                return BadRequest($"Erro ao tentar efeutar a atualização do Employee. {ex.Message}");
            }
        }
    }
    
}
