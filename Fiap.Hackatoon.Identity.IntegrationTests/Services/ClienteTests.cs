using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using Fiap.Hackatoon.Identity.IntegrationTests.Infra;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net;
using System.Text;


namespace Fiap.Hackatoon.Identity.IntegrationTests.Services
{
    public class ClienteTests :  IClassFixture<MyWebApplicationFactory<Program>> 
    {
        private readonly HttpClient _client;
        private readonly MyWebApplicationFactory<Program> _factory; // Use o tipo exato da sua fábrica
        private readonly ITestHarness _testHarness; // Adicione o ITestHarness

        public ClienteTests(MyWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _testHarness = _factory.BusTestHarness; // Obtém o TestHarness da fábrica           
        }



        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "login@cliente.com",
                Password = "Password123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            // Assert
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(token));            
        }

        [Fact]
        public async Task Login_ValidDocumentCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "55566677788",
                Password = "Password123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            // Assert
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "nonexistent@client.com",
                Password = "WrongPassword"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "",
                Password = "Password123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("Email/CPF obrigatório", responseString);
        }


        [Fact]
        public async Task AddClient_NewClient_ReturnsCreated_AndPublishesMessage()
        {            
            // Arrange
            var newClientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Novo Cliente Com Mensagem",
                Email = "message.new@client.com",
                Document = "12345678902",
                Password = "NewPass123",
                ConfirmPassword = "NewPass123",
                Birth = new DateTime(2000, 1, 1)
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(newClientDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/Add", jsonContent);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
   
      
        }

        [Fact]
        public async Task AddClient_ExistingEmailOrDocument_ReturnsBadRequest_AndDoesNotPublishMessage()
        {
            // Arrange
            var existingClientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Cliente Duplicado",
                Email = "existing@cliente.com", // Email já existe no seed
                Document = "98765432100",
                Password = "AnyPassword",
                ConfirmPassword = "AnyPassword",
                Birth = new DateTime(1980, 1, 1)
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(existingClientDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/Add", jsonContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("O email/document já existe cadastrado", responseString);

        }

        [Fact]
        public async Task AddClient_InvalidModel_ReturnsBadRequest_AndDoesNotPublishMessage()
        {
            // Arrange
            var invalidClientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "",
                Email = "invalid",
                Document = "123",
                Password = "123",
                ConfirmPassword = "123",
                Birth = DateTime.MinValue
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(invalidClientDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/Add", jsonContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        }        

        private async Task<string> GetAuthTokenForEmployee()
        {
            var loginDto = new EmployeeLoginDto
            {
                Email = "test@employee.com",
                Password = "EmployeePassword123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Employee/login", jsonContent);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        [Fact]
        public async Task GetClientById_ExistingClient_ReturnsOkWithClientDto()
        {
            // Arrange
            int clientId = 3;
            var token = await GetAuthTokenForEmployee();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync($"/api/Client/GetClientById/{clientId}");

            // Assert
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
            // Arrange
            int nonExistingClientId = 999;
            var token = await GetAuthTokenForEmployee();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync($"/api/Client/GetClientById/{nonExistingClientId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetClientById_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange - Não adiciona cabeçalho de autorização
            int clientId = 1;

            // Act
            var response = await _client.GetAsync($"/api/Client/GetClientById/{clientId}");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }       
    }
}
