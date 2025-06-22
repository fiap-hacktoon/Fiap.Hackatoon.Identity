using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using Fiap.Hackatoon.Identity.Infrastructure.Data;
using Fiap.Hackatoon.Identity.IntegrationTests.Infra;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.IntegrationTests.Services
{
    public class ClienteTests : MyWebApplicationFactory<Program, DbContext>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;
        public ClienteTests(WebApplicationFactory<Program> factory) : base(factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }


        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "test@client.com",
                Password = "Password123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            // Assert
            response.EnsureSuccessStatusCode(); // Status 2xx
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
                EmailOrDocument = "", // Email/CPF obrigatório
                Password = "Password123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/login", jsonContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("Email/CPF obrigatório", responseString); // Verifica mensagem de erro do ModelState
        }


        [Fact]
        public async Task AddClient_NewClient_ReturnsCreated()
        {
            // Arrange
            var newClientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Novo Cliente Integracao",
                Email = "integration.new@client.com",
                Document = "12345678901",
                Password = "NewPass123",
                ConfirmPassword = "NewPass123",
                Birth = new DateTime(1999, 10, 10)
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(newClientDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/Add", jsonContent);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task AddClient_ExistingEmailOrDocument_ReturnsBadRequest()
        {
            // Arrange
            // Usa um cliente que já foi seeded (ou criado por outro teste/semente inicial)
            var existingClientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Cliente Duplicado",
                Email = "test@client.com", // Email já existe no seed
                Document = "98765432100", // Documento diferente, mas o email já causa a exceção
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
        public async Task AddClient_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var invalidClientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "", // Nome obrigatório
                Email = "invalid", // Email inválido
                Document = "123", // Documento inválido (se houver validação de tamanho)
                Password = "123", // Senha muito curta
                ConfirmPassword = "123",
                Birth = DateTime.MinValue // Data de nascimento inválida
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(invalidClientDto), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/Client/Add", jsonContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            // Você pode adicionar mais asserções para verificar as mensagens de erro do ModelState.
            // Ex: var responseString = await response.Content.ReadAsStringAsync();
            // Assert.Contains("O campo nome é obrigatório", responseString);
        }

        // Nota: Para este teste, você precisará de um token de autorização.
        // Em um teste de integração real, você faria um login para obter um token
        // ou mockaria um serviço de token para gerar um token válido para o teste.
        // Por simplicidade, vou demonstrar como seria com um token mockado ou fixo.
        // A geração de um token JWT válido para testes de integração pode ser um pouco complexa
        // porque envolve assinar o token com a mesma chave usada na sua aplicação real.
        // Uma abordagem comum é obter um token de um endpoint de login simulado no próprio teste,
        // ou ter um método no WebApplicationFactory que gere tokens de teste.

        [Fact]
        public async Task GetClientById_ExistingClient_ReturnsOkWithClientDto()
        {
            // Arrange
            // ID do cliente que foi seeded (Id = 2)
            int clientId = 2;
            // Para testes de integração com Authorize, você precisa de um token.
            // O ideal é logar ou criar um token de teste programaticamente.
            // Exemplo Simplificado (NÃO RECOMENDADO para produção sem verificação de assinatura):
            // Você pode ter uma chave JWT de teste na sua WebApplicationFactory para gerar tokens.
            // Ou fazer uma requisição de login para obter um token.
            // Para este exemplo, vou simular um token BEM BÁSICO (não validado, apenas para demonstração da requisição).
            // Em um cenário real, você implementaria um gerador de token no seu factory.

            // 1. O ideal é logar no endpoint de login para obter o token:
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "test@client.com",
                Password = "Password123"
            };
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/Client/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();
            var token = await loginResponse.Content.ReadAsStringAsync();

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


            // Act
            var response = await _client.GetAsync($"/api/Client/GetClientById/{clientId}");

            // Assert
            response.EnsureSuccessStatusCode(); // Status 2xx
            var clientDto = JsonConvert.DeserializeObject<ClientDto>(await response.Content.ReadAsStringAsync());

            Assert.NotNull(clientDto);
            Assert.Equal(TypeRole.Client, clientDto.TypeRole); // Exemplo de verificação de propriedade mapeada
            Assert.Equal("Cliente Buscado", clientDto.Name);
            Assert.Equal("getclient@client.com", clientDto.Email);
        }

        [Fact]
        public async Task GetClientById_NonExistingClient_ReturnsNotFound()
        {
            // Arrange
            int nonExistingClientId = 999;

            // Obter token (mesmo processo do teste anterior)
            var loginDto = new ClientLoginDto
            {
                EmailOrDocument = "test@client.com",
                Password = "Password123"
            };
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/Client/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();
            var token = await loginResponse.Content.ReadAsStringAsync();

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
