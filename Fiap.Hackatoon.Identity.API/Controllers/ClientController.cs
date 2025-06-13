using AutoMapper;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Fiap.Hackatoon.Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IClientApplication _clientApplication;
        private readonly ILogger<ClientController> _logger;
        private readonly IMapper _mapper;

        public ClientController(IClientApplication clientApplication, ILogger<ClientController> logger, IMapper mapper)
        {
            _clientApplication = clientApplication;
            _logger = logger;
            _mapper = mapper;
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

                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Login failed: {clientLoginDto.EmailOrDocument}. Error: ${ex.Message ?? ""}" );
                return BadRequest("Erro ao tentar efeutar o login");                
            }            
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add(ClientDto  clientDto)
        {
            try
            {
                                

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Add cliente failed:. Error: ${ex.Message ?? ""}");
                return BadRequest("Erro ao tentar efeutar o cadastro do cliente.");
                throw;
            }
        }
    }
}
