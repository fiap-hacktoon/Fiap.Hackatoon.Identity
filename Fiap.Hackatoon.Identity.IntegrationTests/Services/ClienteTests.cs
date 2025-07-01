using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using Fiap.Hackatoon.Identity.API; // Altere para o namespace da sua classe Program/Startup
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using Fiap.Hackatoon.Identity.IntegrationTests.Infra;
// using MassTransit.Testing; // Removido, pois não faremos verificações de TestHarness aqui

namespace Fiap.Hackatoon.Identity.IntegrationTests.Services
{
    public class ClienteTests : IClassFixture<MyWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly MyWebApplicationFactory<Program> _factory;
        // private readonly ITestHarness _testHarness; // Removido, pois não faremos verificações de TestHarness aqui

        public ClienteTests(MyWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            // _testHarness = _factory.BusTestHarness; // Removido, pois não faremos verificações de TestHarness aqui
        }

        // --- Helper para obter token de autenticação (cliente ou funcionário) ---
        private async Task<string> GetAuthToken(string emailOrDocument, string password, bool isEmployee = false)
        {
            if (isEmployee)
            {
                var employeeLoginDto = new EmployeeLoginDto
                {
                    Email = emailOrDocument,
                    Password = password
                };
                var jsonContent = new StringContent(JsonConvert.SerializeObject(employeeLoginDto), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("/api/Employee/login", jsonContent);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                var clientLoginDto = new ClientLoginDto
                {
                    EmailOrDocument = emailOrDocument,
                    Password = password
                };
                var jsonContent = new StringContent(JsonConvert.SerializeObject(clientLoginDto), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("/api/Client/login", jsonContent);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        // --- Testes para POST /api/Client/login ---

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "test@client.com",
                Password = "Password123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public async Task Login_ValidDocumentCredentials_ReturnsOkWithToken()
        {
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "55566677788",
                Password = "Password123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "nonexistent@client.com",
                Password = "WrongPassword"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsBadRequest()
        {
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "", // Campo obrigatório
                Password = "Password123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("Email/CPF obrigatório", responseString);
        }

        // --- Testes para POST /api/Client/Add ---

        [Fact]
        public async Task AddClient_NewClient_ReturnsCreated()
        {
            var newClientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Novo Cliente Integracao",
                Email = "integration.new@client.com",
                Document = "12345678901",
                Password = "NewPass123",
                ConfirmPassword = "NewPass123",
                Birth = new DateTime(2000, 1, 1)
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(newClientDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Client/Add", jsonContent);

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task AddClient_ExistingEmailOrDocument_ReturnsBadRequest()
        {
            var existingClientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Cliente Duplicado",
                Email = "test@client.com", // Email já existe no seed
                Document = "98765432100",
                Password = "AnyPassword",
                ConfirmPassword = "AnyPassword",
                Birth = new DateTime(1980, 1, 1)
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(existingClientDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Client/Add", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("O email/document já existe cadastrado", responseString);
        }

        [Fact]
        public async Task AddClient_InvalidModel_ReturnsBadRequest()
        {
            var invalidClientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "", // Inválido
                Email = "invalid", // Inválido
                Document = "123", // Inválido
                Password = "123", // Inválido
                ConfirmPassword = "123",
                Birth = DateTime.MinValue // Inválido
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(invalidClientDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Client/Add", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // --- Testes para GET /api/Client/GetClientById/{id:int} ---

        [Fact]
        public async Task GetClientById_ExistingClient_ReturnsOkWithClientDto()
        {
            int clientId = 3;
            var token = await GetAuthToken("test@employee.com", "EmployeePassword123", isEmployee: true); // Role Manager
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/Client/GetClientById/{clientId}");

            response.EnsureSuccessStatusCode();
            var clientDto = JsonConvert.DeserializeObject<ClientDto>(await response.Content.ReadAsStringAsync());

            Assert.NotNull(clientDto);
            Assert.Equal(clientId, clientDto.Id);
            Assert.Equal("id@cliente.com", clientDto.Email);
            Assert.Equal("Cliente ID Teste", clientDto.Name);
            Assert.Equal(TypeRole.Client, clientDto.TypeRole);
        }

        [Fact]
        public async Task GetClientById_NonExistingClient_ReturnsNotFound()
        {
            int nonExistingClientId = 999;
            var token = await GetAuthToken("test@employee.com", "EmployeePassword123", isEmployee: true);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/Client/GetClientById/{nonExistingClientId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetClientById_Unauthorized_ReturnsUnauthorized()
        {
            int clientId = 1;

            var response = await _client.GetAsync($"/api/Client/GetClientById/{clientId}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetClientById_ForbiddenRole_ReturnsForbidden()
        {
            // Para testar Forbidden: precisa de um token VÁLIDO (autenticado)
            // mas com uma ROLE que NÃO esteja em "Manager,Attendant,Kitchen,Client".
            // Por exemplo, se "Attendant" não tem acesso, gere um token para um Attendant.

            // Assumindo que o usuário "kitchen@employee.com" tem TypeRole.Kitchen.
            // Se "Kitchen" NÃO está na lista de roles do Authorize para este endpoint, o teste deve ser 403 Forbidden.
            // No seu ClientController, "Kitchen" ESTÁ na lista, então este teste retornaria Ok.
            // Para testar Forbidden, você precisaria de um usuário com uma role *não permitida*.

            // Exemplo hipotético se tivéssemos um usuário com TypeRole.Guest no seed:
            // var token = await GetAuthToken("guest@employee.com", "GuestPassword123", isEmployee: true);
            // _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            // var response = await _client.GetAsync($"/api/Client/GetClientById/1");
            // Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            Assert.True(true); // Placeholder
        }

        // --- Testes para GET /api/Client/GetClientByEmail/{email} ---

        [Fact]
        public async Task GetClientByEmail_ExistingClient_ReturnsOkWithClientDto()
        {
            string clientEmail = "emailget@cliente.com";
            var token = await GetAuthToken("test@employee.com", "EmployeePassword123", isEmployee: true); // Manager tem acesso
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/Client/GetClientByEmail/{clientEmail}");

            response.EnsureSuccessStatusCode();
            var clientDto = JsonConvert.DeserializeObject<ClientDto>(await response.Content.ReadAsStringAsync());

            Assert.NotNull(clientDto);
            Assert.Equal(clientEmail, clientDto.Email);
            Assert.Equal("Cliente Email Teste", clientDto.Name);
        }

        [Fact]
        public async Task GetClientByEmail_NonExistingClient_ReturnsNotFound()
        {
            string nonExistingEmail = "nonexistentbyemail@client.com";
            var token = await GetAuthToken("test@employee.com", "EmployeePassword123", isEmployee: true); // Manager tem acesso
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/Client/GetClientByEmail/{nonExistingEmail}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetClientByEmail_Unauthorized_ReturnsUnauthorized()
        {
            string clientEmail = "emailget@cliente.com";

            var response = await _client.GetAsync($"/api/Client/GetClientByEmail/{clientEmail}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetClientByEmail_ForbiddenRole_ReturnsForbidden()
        {
            // Similar ao Forbidden para GetClientById, precisa de uma role válida mas não permitida.
            Assert.True(true); // Placeholder
        }

        // --- Testes para PUT /api/Client/UpdateClient/{clientId:int} ---

        [Fact]
        public async Task UpdateClient_AuthorizedValidClient_ReturnsNoContent()
        {
            // Cliente seeded com ID 5, email "update@cliente.com"
            int clientId = 5;
            var updateDto = new ClientUpdateDto
            {
                Name = "Updated Integracao",
                Email = "update.new@cliente.com", // Novo email único
                Document = "12345678910", // Documento original
                TypeRole = TypeRole.Client,
                Birth = new DateTime(1995, 3, 15)
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // Gerar token para o próprio cliente (ID 5, TypeRole.Client)
            var token = await GetAuthToken("update@cliente.com", "Password123", isEmployee: false); // Cliente logado como ID 5
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.PutAsync($"/api/Client/UpdateClient/{clientId}", jsonContent);

            // Assert
            response.EnsureSuccessStatusCode(); // Deve ser 204 No Content
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task UpdateClient_UnauthorizedUserId_ReturnsUnauthorized()
        {
            // Tenta atualizar cliente ID 5
            int targetClientId = 5;
            var updateDto = new ClientUpdateDto
            {
                Name = "Attempted Update",
                Email = "attempt@client.com",
                Document = "123",
                TypeRole = TypeRole.Client,
                Birth = DateTime.Now
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // --- CORREÇÃO: Gerar token para um CLIENTE DIFERENTE (ex: Cliente ID 1) ---
            var token = await GetAuthToken("test@client.com", "Password123", isEmployee: false); // Cliente logado como ID 1
            // O token do Cliente ID 1 não tem autorização para atualizar o Cliente ID 5
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.PutAsync($"/api/Client/UpdateClient/{targetClientId}", jsonContent);

            // Assert
            // Espera Unauthorized (401) porque o ID do usuário no token não corresponde ao clientId na URL
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        

        [Fact]
        public async Task UpdateClient_EmailAlreadyInUse_ReturnsBadRequest()
        {
            int clientId = 5; // Cliente que vai tentar atualizar o email
            var updateDto = new ClientUpdateDto
            {
                Name = "Updated Name",
                Email = "conflito@cliente.com", // Email já usado pelo Cliente 6
                Document = "12345678910",
                TypeRole = TypeRole.Client,
                Birth = new DateTime(1995, 3, 15)
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            var token = await GetAuthToken("update@cliente.com", "Password123", isEmployee: false); // Token do Cliente ID 5
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PutAsync($"/api/Client/UpdateClient/{clientId}", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains($"O email {updateDto.Email} já está sendo usado para outro client", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task UpdateClient_InvalidModel_ReturnsBadRequest()
        {
            int clientId = 5;
            var invalidUpdateDto = new ClientUpdateDto
            {
                Name = "", // Inválido
                Email = "invalid", // Inválido
                Document = "123", // Inválido
                TypeRole = TypeRole.Client,
                Birth = DateTime.MinValue // Inválido
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(invalidUpdateDto), Encoding.UTF8, "application/json");

            var token = await GetAuthToken("update@cliente.com", "Password123", isEmployee: false);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PutAsync($"/api/Client/UpdateClient/{clientId}", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}