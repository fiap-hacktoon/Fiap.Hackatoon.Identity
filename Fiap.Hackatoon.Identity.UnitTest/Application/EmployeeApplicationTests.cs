using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using MassTransit;
using Moq;


namespace Fiap.Hackatoon.Identity.IntegrationTests.Application
{
    public class EmployeeApplicationTests
    {
        private readonly Mock<IEmployeeService> _mockEmployeeService;
        private readonly Mock<ITokenApplication> _mockTokenApplication;
        private readonly Mock<IBus> _mockBus;
        private readonly EmployeeApplication _employeeApplication;

        public EmployeeApplicationTests()
        {
            _mockEmployeeService = new Mock<IEmployeeService>();
            _mockTokenApplication = new Mock<ITokenApplication>();
            _mockBus = new Mock<IBus>();
            _employeeApplication = new EmployeeApplication(
                _mockEmployeeService.Object,
                _mockBus.Object,
                _mockTokenApplication.Object
            );
        }



        [Fact]
        public async Task Login_ShouldReturnToken_WhenEmployeeIsValid()
        {
            // Arrange
            string email = "employee@example.com";
            string password = "empPassword123";
            // Usamos a entidade Employee, que herda de User
           var employee = new Employee { Id = 1, Email = email, Password = password, TypeRole = TypeRole.Manager, Name = "Nome do Funcionário" };
            string expectedToken = "mocked_employee_jwt_token";

            _mockEmployeeService.Setup(s => s.GetEmployeeLogin(email, password)).ReturnsAsync(employee);
            _mockTokenApplication.Setup(t => t.GenerateToken(employee)).Returns(expectedToken);

            // Act
            var result = await _employeeApplication.Login(email, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedToken, result);
            _mockEmployeeService.Verify(s => s.GetEmployeeLogin(email, password), Times.Once);
            _mockTokenApplication.Verify(t => t.GenerateToken(employee), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnNull_WhenEmployeeIsInvalid()
        {
            // Arrange
            string email = "invalid@example.com";
            string password = "wrongpassword";

            _mockEmployeeService.Setup(s => s.GetEmployeeLogin(email, password)).ReturnsAsync((Employee)null); // Retorna null

            // Act
            var result = await _employeeApplication.Login(email, password);

            // Assert
            Assert.Null(result);
            _mockEmployeeService.Verify(s => s.GetEmployeeLogin(email, password), Times.Once);
            _mockTokenApplication.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never); // Nenhuma tentativa de gerar token
        }

        [Fact]
        public async Task Login_ShouldThrowException_WhenServiceThrowsException()
        {
            // Arrange
            string email = "error@example.com";
            string password = "password123";

            _mockEmployeeService.Setup(s => s.GetEmployeeLogin(email, password)).ThrowsAsync(new Exception("Network error during login"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _employeeApplication.Login(email, password));
            _mockEmployeeService.Verify(s => s.GetEmployeeLogin(email, password), Times.Once);
            _mockTokenApplication.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }

   

        [Fact]
        public async Task AddEmployee_ShouldReturnTrueAndPublishMessage_WhenEmployeeDoesNotExist()
        {
            // Arrange
            var employeeCreateDto = new EmployeeCreateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Novo Funcionário",
                Email = "newemployee@example.com",
                Password = "EmpSecurePassword123",
                ConfirmPassword = "EmpSecurePassword123"
            };

            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeCreateDto.Email)).ReturnsAsync((Employee)null);
            _mockBus.Setup(b => b.Publish(It.IsAny<EmployeeCreateDto>(), default)).Returns(Task.CompletedTask);

            // Act
            var result = await _employeeApplication.AddEmployee(employeeCreateDto);

            // Assert
            Assert.True(result);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeCreateDto.Email), Times.Once);
            // Verifica se o método Publish foi chamado com o DTO correto
            _mockBus.Verify(b => b.Publish(It.Is<EmployeeCreateDto>(dto =>
                dto.Email == employeeCreateDto.Email), default), Times.Once);
        }

        [Fact]
        public async Task AddEmployee_ShouldThrowException_WhenEmployeeAlreadyExists()
        {
            // Arrange
            var employeeCreateDto = new EmployeeCreateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Funcionário Existente",
                Email = "existingemployee@example.com",
                Password = "ExistingEmpPassword",
                ConfirmPassword = "ExistingEmpPassword"
            };
            // Retorna um funcionário existente para simular o cenário de duplicidade
            var existingEmployee = new Employee { Id = 20, Email = employeeCreateDto.Email };

            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeCreateDto.Email)).ReturnsAsync(existingEmployee);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _employeeApplication.AddEmployee(employeeCreateDto));
            Assert.Equal("O email já existe cadastrado", exception.Message);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeCreateDto.Email), Times.Once);
            _mockBus.Verify(b => b.Publish(It.IsAny<EmployeeCreateDto>(), default), Times.Never); // Não deve publicar se o funcionário já existe
        }

        [Fact]
        public async Task AddEmployee_ShouldThrowException_WhenBusPublishFails()
        {
            // Arrange
            var employeeCreateDto = new EmployeeCreateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Falha Publicacao",
                Email = "publishfailemployee@example.com",
                Password = "PublishFailEmpPassword",
                ConfirmPassword = "PublishFailEmpPassword"
            };

            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeCreateDto.Email)).ReturnsAsync((Employee)null);
            _mockBus.Setup(b => b.Publish(It.IsAny<EmployeeCreateDto>(), default)).ThrowsAsync(new Exception("MassTransit publish error for employee"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _employeeApplication.AddEmployee(employeeCreateDto));
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeCreateDto.Email), Times.Once);
            _mockBus.Verify(b => b.Publish(It.IsAny<EmployeeCreateDto>(), default), Times.Once); // Verifica se a tentativa de publicação ocorreu
        }
    }
}
