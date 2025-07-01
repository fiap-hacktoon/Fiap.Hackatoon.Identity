using AutoMapper;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Net;

namespace Fiap.Hackatoon.Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IClientApplication _clientApplication;
        private readonly IClientService _clientService;
        private readonly ILogger<ClientController> _logger;
        private readonly IMapper _mapper;

        public ClientController(IClientApplication clientApplication, ILogger<ClientController> logger, IMapper mapper, IClientService clientService)
        {
            _clientApplication = clientApplication;
            _logger = logger;
            _mapper = mapper;
            _clientService = clientService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ClientLoginDto clientLoginDto)
        {
            _logger.LogInformation($"Start Login: {clientLoginDto.EmailOrDocument}");
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                _logger.LogInformation($"Login: {clientLoginDto.EmailOrDocument}");
                var token = await _clientApplication.Login(clientLoginDto.EmailOrDocument, clientLoginDto.Password);

                if (string.IsNullOrEmpty(token))
                    return Unauthorized();

                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Login failed: {clientLoginDto.EmailOrDocument}. Error: ${ex.Message ?? ""}");
                return BadRequest("Erro ao tentar efeutar o login");
            }
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add(ClientCreateDto clientDto)
        {
            _logger.LogInformation($"Start add client: {clientDto.Email}");
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var addClient = await _clientApplication.AddClient(clientDto);

                if (addClient)
                    return StatusCode(201);
                else
                    return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Add cliente failed:. Error: ${ex.Message ?? ""}");
                return BadRequest($"Erro ao tentar efeutar o cadastro do cliente. {ex.Message}");
            }
        }


        [Authorize(Roles = "Manager,Attendant,Kitchen,Client")]
        [HttpGet("GetClientById/{id:int}")]
        public async Task<IActionResult> GetClientById(int id)
        {
            _logger.LogInformation($"Start GetClientById: {id}");
            try
            {
                var client = await _clientService.GetClientById(id);

                if (client is not null)
                    return Ok(_mapper.Map<ClientDto>(client));
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error GetClientById ${ex.Message ?? ""}");
                throw;
            }
        }

        [Authorize(Roles = "Manager,Kitchen")]
        [HttpGet("GetClientByEmail/{email}")]
        public async Task<IActionResult> GetClientByEmail(string email)
        {
            _logger.LogInformation($"Start GetClientByEmail: {email}");
            try
            {
                var client = await _clientService.GetClientByEmail(email);

                if (client is not null)
                    return Ok(_mapper.Map<ClientDto>(client));
                else
                    return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error GetClientByEmail ${ex.Message ?? ""}");
                throw;
            }
        }

        [Authorize(Roles = "Client")]
        [HttpPut("UpdateClient/{clientId:int}")]        
        public async Task<IActionResult> UpdateClient(int clienteId, [FromBody] ClientUpdateDto clientUpdateDto)
        {
            _logger.LogInformation($"update client: {clientUpdateDto.Email}");
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var addClient = await _clientApplication.UpdateClient(clienteId, clientUpdateDto);

                if (addClient)
                    return NoContent();
                else
                    return BadRequest();
            }            
            catch (Exception ex)
            {
                _logger.LogInformation($"Update cliente failed:. Error: ${ex.Message ?? ""}");
                return BadRequest($"Erro ao tentar efeutar a atuazliação do cliente. {ex.Message}");
            }
        }
    }
}
