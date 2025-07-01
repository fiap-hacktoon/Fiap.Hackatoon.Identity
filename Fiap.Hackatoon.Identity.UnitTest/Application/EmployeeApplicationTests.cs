using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using MassTransit; // Ainda necessário para IBus (se outras classes usarem)
using Microsoft.Extensions.Options; // Para mockar IOptions
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit; // Adicione este using para XUnit.

namespace Fiap.Hackatoon.Identity.UnitTest.Application
{
    public class EmployeeApplicationTests
    {
        private readonly Mock<IEmployeeService> _mockEmployeeService;
        private readonly Mock<ITokenApplication> _mockTokenApplication;
        private readonly Mock<IBusService> _mockBusService; // Mocado agora IBusService
        private readonly Mock<IOptions<RabbitMqConnection>> _mockRabbitMqOptions; // Novo mock para IOptions
        private readonly EmployeeApplication _employeeApplication;

        // Nomes de fila que serão configurados no mock de RabbitMqConnection
        private readonly string _employeeCreateQueueName = "fila_funcionario_criado";
        private readonly string _employeeUpdateQueueName = "fila_funcionario_atualizado";

        public EmployeeApplicationTests()
        {
            _mockEmployeeService = new Mock<IEmployeeService>();
            _mockTokenApplication = new Mock<ITokenApplication>();
            _mockBusService = new Mock<IBusService>(); // Instancia o mock para IBusService
            _mockRabbitMqOptions = new Mock<IOptions<RabbitMqConnection>>(); // Instancia o mock para IOptions

            // Configura o mock de IOptions<RabbitMqConnection>
            _mockRabbitMqOptions.Setup(o => o.Value).Returns(new RabbitMqConnection
            {
                QueueNameEmployeeCreate = _employeeCreateQueueName,
                QueueNameEmployeeUpdate = _employeeUpdateQueueName,
                // Adicione outras propriedades de RabbitMqConnection se a EmployeeApplication as utilizar
                HostName = "localhost",
                Port = "5672",
                UserName = "guest",
                Password = "guest"
            });

            _employeeApplication = new EmployeeApplication(
                _mockEmployeeService.Object,
                _mockBusService.Object, // Passa o mock de IBusService
                _mockTokenApplication.Object,
                _mockRabbitMqOptions.Object // Passa o mock de IOptions
            );
        }


        [Fact]
        public async Task Login_ShouldReturnToken_WhenEmployeeIsValid()
        {
            // Arrange
            string email = "employee@example.com";
            string password = "empPassword123";
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

            _mockEmployeeService.Setup(s => s.GetEmployeeLogin(email, password)).ReturnsAsync((Employee)null);

            // Act
            var result = await _employeeApplication.Login(email, password);

            // Assert
            Assert.Null(result);
            _mockEmployeeService.Verify(s => s.GetEmployeeLogin(email, password), Times.Once);
            _mockTokenApplication.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
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
        public async Task AddEmployee_ShouldReturnTrueAndSendToBus_WhenEmployeeDoesNotExist()
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
            // Configura o _mockBusService.SendToBus
            _mockBusService.Setup(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _employeeApplication.AddEmployee(employeeCreateDto);

            // Assert
            Assert.True(result);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeCreateDto.Email), Times.Once);
            // Verifica se SendToBus foi chamado com o DTO correto e o nome da fila correto
            _mockBusService.Verify(b => b.SendToBus(
                It.Is<EmployeeCreateDto>(dto =>
                    dto.Email == employeeCreateDto.Email &&
                    dto.Name == employeeCreateDto.Name), // Assumindo EventDto tem Email e Name
                _employeeCreateQueueName), // Usa o nome da fila configurado no mock de IOptions
                Times.Once);
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
            var existingEmployee = new Employee { Id = 20, Email = employeeCreateDto.Email };

            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeCreateDto.Email)).ReturnsAsync(existingEmployee);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _employeeApplication.AddEmployee(employeeCreateDto));
            Assert.Equal("O email já existe cadastrado", exception.Message);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeCreateDto.Email), Times.Once);
            // Verifica que SendToBus NUNCA foi chamado
            _mockBusService.Verify(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>()), Times.Never);
        }
       

        [Fact]
        public async Task UpdateEmployee_ShouldReturnTrueAndSendToBus_WhenEmployeeExistsAndEmailNotChanged()
        {
            // Arrange
            int employeeId = 1;
            var employeeUpdateDto = new EmployeeUpdateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Emp Name",
                Email = "originalemp@example.com"
            };
            var existingEmployee = new Employee { Id = employeeId, Email = "originalemp@example.com" };

            _mockEmployeeService.Setup(s => s.GetEmployeeById(employeeId)).ReturnsAsync(existingEmployee);            

            // Act
            var result = await _employeeApplication.UpdateEmployee(employeeId, employeeUpdateDto);

            // Assert
            Assert.True(result);
            _mockEmployeeService.Verify(s => s.GetEmployeeById(employeeId), Times.Once);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(It.IsAny<string>()), Times.Never); // Email não mudou, não deve chamar
            _mockBusService.Verify(b => b.SendToBus(
                It.Is<EmployeeUpdateDto>(dto =>
                    dto.Email == employeeUpdateDto.Email &&
                    dto.Name == employeeUpdateDto.Name),
                _employeeUpdateQueueName), // Usa o nome da fila configurado
                Times.Once);
        }

        [Fact]
        public async Task UpdateEmployee_ShouldReturnTrueAndSendToBus_WhenEmployeeExistsAndEmailChangedAndNewEmailIsUnique()
        {
            // Arrange
            int employeeId = 1;
            var employeeUpdateDto = new EmployeeUpdateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Emp Name",
                Email = "newuniqueemp@example.com"
            };
            var existingEmployee = new Employee { Id = employeeId, Email = "oldemp@example.com" };

            _mockEmployeeService.Setup(s => s.GetEmployeeById(employeeId)).ReturnsAsync(existingEmployee);
            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeUpdateDto.Email)).ReturnsAsync((Employee)null); // Novo email é único
            _mockBusService.Setup(b => b.SendToBus(It.IsAny<EmployeeCreateDto>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _employeeApplication.UpdateEmployee(employeeId, employeeUpdateDto);

            // Assert
            Assert.True(result);
            _mockEmployeeService.Verify(s => s.GetEmployeeById(employeeId), Times.Once);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeUpdateDto.Email), Times.Once); // Deve verificar o novo email
            
        }

        [Fact]
        public async Task UpdateEmployee_ShouldThrowException_WhenEmployeeNotFound()
        {
            // Arrange
            int employeeId = 999;
            var employeeUpdateDto = new EmployeeUpdateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Name",
                Email = "email@example.com"
            };

            _mockEmployeeService.Setup(s => s.GetEmployeeById(employeeId)).ReturnsAsync((Employee)null); // Funcionário não encontrado

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _employeeApplication.UpdateEmployee(employeeId, employeeUpdateDto));
            Assert.Equal($"Employee com id:{employeeId} não encontrado", exception.Message);
            _mockEmployeeService.Verify(s => s.GetEmployeeById(employeeId), Times.Once);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(It.IsAny<string>()), Times.Never);
            
        }

        [Fact]
        public async Task UpdateEmployee_ShouldThrowException_WhenNewEmailAlreadyUsed()
        {
            // Arrange
            int employeeId = 1;
            var employeeUpdateDto = new EmployeeUpdateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Name",
                Email = "alreadyused@example.com"
            };
            var existingEmployee = new Employee { Id = employeeId, Email = "originalemp@example.com" };
            var employeeWithExistingEmail = new Employee { Id = 2, Email = "alreadyused@example.com" }; // Outro funcionário usando o email

            _mockEmployeeService.Setup(s => s.GetEmployeeById(employeeId)).ReturnsAsync(existingEmployee);
            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeUpdateDto.Email)).ReturnsAsync(employeeWithExistingEmail); // Email já está em uso

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _employeeApplication.UpdateEmployee(employeeId, employeeUpdateDto));
            Assert.Equal($"O email {employeeUpdateDto.Email} já está sendo usado para outro employee", exception.Message);
            _mockEmployeeService.Verify(s => s.GetEmployeeById(employeeId), Times.Once);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeUpdateDto.Email), Times.Once);
            
        }

        [Fact]
        public async Task UpdateEmployee_ShouldThrowException_WhenBusServiceSendFails()
        {
            // Arrange
            int employeeId = 1;
            var employeeUpdateDto = new EmployeeUpdateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Name",
                Email = "uniqueemp@example.com"
            };
            var existingEmployee = new Employee { Id = employeeId, Email = "originalemp@example.com" };

            _mockEmployeeService.Setup(s => s.GetEmployeeById(employeeId)).ReturnsAsync(existingEmployee);
            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeUpdateDto.Email)).ReturnsAsync((Employee)null);
            _mockBusService.Setup(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>())).ThrowsAsync(new Exception("BusService update send error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _employeeApplication.UpdateEmployee(employeeId, employeeUpdateDto));
            Assert.Equal("BusService update send error", exception.Message);
            _mockEmployeeService.Verify(s => s.GetEmployeeById(employeeId), Times.Once);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeUpdateDto.Email), Times.Once);
            _mockBusService.Verify(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>()), Times.Once);
        }
    }
}