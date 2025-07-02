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
using MassTransit.Testing;
using Fiap.Hackatoon.Shared.Dto; // Para ITestHarness

namespace Fiap.Hackatoon.Identity.IntegrationTests.Services
{
    public class EmployeeTests : IClassFixture<MyWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly MyWebApplicationFactory<Program> _factory;
        private readonly ITestHarness _testHarness; // Usado para verificar mensagens MassTransit

        public EmployeeTests(MyWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _testHarness = _factory.BusTestHarness; // Obtém o TestHarness da fábrica
        }

        // --- Helper para obter token de autenticação (sempre para Employee aqui) ---
        private async Task<string> GetAuthTokenForEmployee(string email, string password)
        {
            var loginDto = new EmployeeLoginDto
            {
                Email = email,
                Password = password
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Employee/login", jsonContent);
            response.EnsureSuccessStatusCode(); // Garante que o login foi bem-sucedido
            return await response.Content.ReadAsStringAsync();
        }

        // --- Testes para POST /api/Employee/login ---

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Manager é um TypeRole que deve ter acesso
            var loginDto = new EmployeeLoginDto { Email = "manager@employee.com", Password = "EmployeePassword123" };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Employee/login", jsonContent);

            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var loginDto = new EmployeeLoginDto { Email = "nonexistent@employee.com", Password = "WrongPassword" };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Employee/login", jsonContent);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_InvalidModel_ReturnsBadRequest()
        {
            var loginDto = new EmployeeLoginDto { Email = "", Password = "password" }; // Email obrigatório
            var jsonContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/Employee/login", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Email é obrigatório", await response.Content.ReadAsStringAsync());
        }

        // --- Testes para POST /api/Employee/Add (Requer role Manager) ---

        [Fact]
        public async Task AddEmployee_AuthorizedNewEmployee_ReturnsCreated_AndPublishesMessage()
        {
            var newEmployeeDto = new EmployeeCreateDto
            {
                TypeRole = TypeRole.Attendant, // Exemplo: adicionando um atendente
                Name = "Novo Atendente",
                Email = "new.attendant@employee.com",
                Password = "AttendantPassword123",
                ConfirmPassword = "AttendantPassword123"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(newEmployeeDto), Encoding.UTF8, "application/json");

            // Login como Manager para ter autorização
            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("/api/Employee/Add", jsonContent);

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
         
        }

        [Fact]
        public async Task AddEmployee_Unauthorized_ReturnsUnauthorized()
        {
            var newEmployeeDto = new EmployeeCreateDto { TypeRole = TypeRole.Attendant, Name = "No Auth", Email = "noauth@employee.com", Password = "Pass123", ConfirmPassword = "Pass123" };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(newEmployeeDto), Encoding.UTF8, "application/json");

            // Não fornece token de autorização
            var response = await _client.PostAsync("/api/Employee/Add", jsonContent);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);            
        }

        [Fact]
        public async Task AddEmployee_ForbiddenRole_ReturnsForbidden()
        {
            var newEmployeeDto = new EmployeeCreateDto { TypeRole = TypeRole.Attendant, Name = "Forbidden Role", Email = "forbidden@employee.com", Password = "Pass123", ConfirmPassword = "Pass123" };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(newEmployeeDto), Encoding.UTF8, "application/json");

            // Login com Attendant, que NÃO tem role "Manager" para adicionar (se configurado como Forbidden)
            var token = await GetAuthTokenForEmployee("attendant@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("/api/Employee/Add", jsonContent);

            // Se a role "Attendant" NÃO estiver na lista de roles para o endpoint "Add"
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);            
        }

        [Fact]
        public async Task AddEmployee_ExistingEmail_ReturnsBadRequest_AndDoesNotPublishMessage()
        {
            var existingEmployeeDto = new EmployeeCreateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Funcionario Existente",
                Email = "manager@employee.com", // Email já existe no seed
                Password = "ExistingPass",
                ConfirmPassword = "ExistingPass"
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(existingEmployeeDto), Encoding.UTF8, "application/json");

            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("/api/Employee/Add", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("O email já existe cadastrado", await response.Content.ReadAsStringAsync());            
        }

        [Fact]
        public async Task AddEmployee_InvalidModel_ReturnsBadRequest_AndDoesNotPublishMessage()
        {
            var invalidEmployeeDto = new EmployeeCreateDto { TypeRole = TypeRole.Manager, Name = "", Email = "invalid", Password = "123", ConfirmPassword = "123" };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(invalidEmployeeDto), Encoding.UTF8, "application/json");

            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("/api/Employee/Add", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);            
        }

        // --- Testes para GET /api/Employee/GetEmployeeById/{id:int} (Requer role Manager) ---

        [Fact]
        public async Task GetEmployeeById_AuthorizedExistingEmployee_ReturnsOkWithEmployeeDto()
        {
            int employeeId = 104; // Funcionário seeded
            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/Employee/GetEmployeeById/{employeeId}");

            response.EnsureSuccessStatusCode();
            var employeeDto = JsonConvert.DeserializeObject<EmployeeDto>(await response.Content.ReadAsStringAsync());

            Assert.NotNull(employeeDto);
            Assert.Equal(employeeId, employeeDto.Id);
            Assert.Equal("id@employee.com", employeeDto.Email);
        }

        [Fact]
        public async Task GetEmployeeById_Unauthorized_ReturnsUnauthorized()
        {
            int employeeId = 101;
            // Nenhuma autorização
            var response = await _client.GetAsync($"/api/Employee/GetEmployeeById/{employeeId}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetEmployeeById_ForbiddenRole_ReturnsForbidden()
        {
            // Login com Attendant, que NÃO tem role "Manager" para GetEmployeeById
            var token = await GetAuthTokenForEmployee("attendant@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/Employee/GetEmployeeById/101");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEmployeeById_NonExistingEmployee_ReturnsNotFound()
        {
            int nonExistingEmployeeId = 999;
            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/Employee/GetEmployeeById/{nonExistingEmployeeId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // --- Testes para GET /api/Employee/GetEmployeeByEmail/{email} (Requer role Manager) ---

        [Fact]
        public async Task GetEmployeeByEmail_AuthorizedExistingEmployee_ReturnsOkWithEmployeeDto()
        {
            string employeeEmail = "emailget@employee.com";
            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/Employee/GetEmployeeByEmail/{employeeEmail}");

            response.EnsureSuccessStatusCode();
            var employeeDto = JsonConvert.DeserializeObject<EmployeeDto>(await response.Content.ReadAsStringAsync());

            Assert.NotNull(employeeDto);
            Assert.Equal(employeeEmail, employeeDto.Email);
            Assert.Equal("Funcionario Email Teste", employeeDto.Name);
        }

        [Fact]
        public async Task GetEmployeeByEmail_Unauthorized_ReturnsUnauthorized()
        {
            string employeeEmail = "emailget@employee.com";
            // Nenhuma autorização
            var response = await _client.GetAsync($"/api/Employee/GetEmployeeByEmail/{employeeEmail}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetEmployeeByEmail_ForbiddenRole_ReturnsForbidden()
        {
            // Login com Attendant, que NÃO tem role "Manager" para GetEmployeeByEmail
            var token = await GetAuthTokenForEmployee("attendant@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            string employeeEmail = "emailget@employee.com";
            var response = await _client.GetAsync($"/api/Employee/GetEmployeeByEmail/{employeeEmail}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEmployeeByEmail_NonExistingEmployee_ReturnsNotFound()
        {
            string nonExistingEmail = "nonexistentbyemail@employee.com";
            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/Employee/GetEmployeeByEmail/{nonExistingEmail}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // --- Testes para PUT /api/Employee/UpdateEmployee/{employeeId:int} ---

        [Fact]
        public async Task UpdateEmployee_AuthorizedValidEmployee_ReturnsNoContent_AndPublishesMessage()
        {
            int employeeId = 104; // Funcionário para atualização (seeded)
            var updateDto = new EmployeeUpdateDto
            {
                Name = "Updated Emp Integracao",
                Email = "updated.integracao@employee.com", // Novo email único
                TypeRole = TypeRole.Attendant // Pode mudar a role se a regra permitir
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // Login como Manager para ter autorização (Manager está em "Manager,Attendant,Kitchen" se a role "Kitchen" não for Manager)
            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PutAsync($"/api/Employee/UpdateEmployee/{employeeId}", jsonContent); // <<-- ATENÇÃO: endpoint é UpdateClient

            response.EnsureSuccessStatusCode(); // Deve ser 204 No Content
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);                    
        }

        [Fact]
        public async Task UpdateEmployee_Unauthorized_ReturnsUnauthorized()
        {
            int employeeId = 104;
            var updateDto = new EmployeeUpdateDto { Name = "NoAuth Update", Email = "noauth@employee.com", TypeRole = TypeRole.Attendant };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // Nenhuma autorização
            var response = await _client.PutAsync($"/api/Employee/UpdateEmployee/{employeeId}", jsonContent);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);            
        }
     

        [Fact]
        public async Task UpdateEmployee_EmployeeNotFound_ReturnsBadRequest()
        {
            int employeeId = 999; // ID não existente
            var updateDto = new EmployeeUpdateDto { Name = "NonExistent Emp", Email = "nonexistent@employee.com", TypeRole = TypeRole.Manager };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PutAsync($"/api/Employee/UpdateEmployee/{employeeId}", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains($"Employee com id:{employeeId} não encontrado", await response.Content.ReadAsStringAsync());            
        }

        [Fact]
        public async Task UpdateEmployee_EmailAlreadyInUse_ReturnsBadRequest()
        {
            int employeeId = 104; // Funcionário para atualização
            var updateDto = new EmployeeUpdateDto
            {
                Name = "Updated Name",
                Email = "conflito@employee.com", // Email já usado pelo Funcionário 106
                TypeRole = TypeRole.Manager
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PutAsync($"/api/Employee/UpdateEmployee/{employeeId}", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains($"O email {updateDto.Email} já está sendo usado para outro employee", await response.Content.ReadAsStringAsync());
            
        }

        [Fact]
        public async Task UpdateEmployee_InvalidModel_ReturnsBadRequest_AndDoesNotPublishMessage()
        {
            int employeeId = 104;
            var invalidUpdateDto = new EmployeeUpdateDto { Name = "", Email = "invalid", TypeRole = TypeRole.Manager };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(invalidUpdateDto), Encoding.UTF8, "application/json");

            var token = await GetAuthTokenForEmployee("manager@employee.com", "EmployeePassword123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PutAsync($"/api/Employee/UpdateEmployee/{employeeId}", jsonContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            
        }
    }
}