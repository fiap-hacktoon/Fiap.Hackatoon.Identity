using Moq;
using Xunit;
using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using MassTransit; // Para IBus e ISendEndpoint
using Microsoft.Extensions.Options; // Para IOptions
using System.Threading.Tasks;
using System;
using Fiap.Hackatoon.Shared.Dto;
using AutoMapper;

namespace Fiap.Hackatoon.Identity.UnitTest.Application
{
    public class EmployeeApplicationTests
    {
        private readonly Mock<IEmployeeService> _mockEmployeeService;
        private readonly Mock<ITokenApplication> _mockTokenApplication;
        private readonly Mock<IBus> _mockBus; // Mock do IBus (direto)
        private readonly Mock<IOptions<RabbitMqConnection>> _mockRabbitMqOptions;
        private readonly EmployeeApplication _employeeApplication;
        private readonly Mock<IMapper> _mapper;

        // Nomes de fila que serão configurados no mock de RabbitMqConnection
        private readonly string _employeeCreateQueueName = "fila_funcionario_criado";
        private readonly string _employeeUpdateQueueName = "fila_funcionario_atualizado";

        public EmployeeApplicationTests()
        {
            _mockEmployeeService = new Mock<IEmployeeService>();
            _mockTokenApplication = new Mock<ITokenApplication>();
            _mockBus = new Mock<IBus>(); // Instancia o mock para IBus
            _mockRabbitMqOptions = new Mock<IOptions<RabbitMqConnection>>();
            _mapper = new Mock<IMapper>();


            // Configura o mock do IOptions<RabbitMqConnection>
            _mockRabbitMqOptions.Setup(o => o.Value).Returns(new RabbitMqConnection
            {
                QueueNameEmployeeCreate = _employeeCreateQueueName,
                QueueNameEmployeeUpdate = _employeeUpdateQueueName,
                // Outras propriedades podem ser configuradas se usadas no código
                HostName = "localhost",
                Port = "5672",
                UserName = "guest",
                Password = "guest"
            });

            _employeeApplication = new EmployeeApplication(
                _mockEmployeeService.Object,
                _mockBus.Object, // Passa o mock de IBus
                _mockTokenApplication.Object,
                _mockRabbitMqOptions.Object,
                _mapper.Object
            );

            // Setup inicial para o GetSendEndpoint do IBus.
            // Para cada chamada Send, MassTransit.IBus.GetSendEndpoint é chamado primeiro.
            // Precisamos mockar isso para retornar um ISendEndpoint mockado.
            _mockBus.Setup(b => b.GetSendEndpoint(It.IsAny<Uri>()))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>()); // Retorna um mock de ISendEndpoint
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
            var expected = new EmployeeCreateEvent
            {
                TypeRole = TypeRole.Manager,
                Name = "Novo Funcionário",
                Email = "newemployee@example.com",
                Password = "EmpSecurePassword123",                
            };

            var employeeCreateDto = new EmployeeCreateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Novo Funcionário",
                Email = "newemployee@example.com",
                Password = "EmpSecurePassword123",
                ConfirmPassword = "EmpSecurePassword123"
            };

            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeCreateDto.Email)).ReturnsAsync((Employee)null);

            // Certifique-se que o mock de ISendEndpoint.Send também está configurado
            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeCreateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<EmployeeCreateDto>(), It.IsAny<CancellationToken>()) == Task.CompletedTask));

            _mapper.Setup(x => x.Map<EmployeeCreateEvent>(It.IsAny<EmployeeCreateDto>())).Returns(expected);

            // Act
            var result = await _employeeApplication.AddEmployee(employeeCreateDto);

            // Assert
            Assert.True(result);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeCreateDto.Email), Times.Once);

            // Verifica a chamada a GetSendEndpoint com a URI correta
            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeCreateQueueName}")), Times.Once);
            // Verifica a chamada a Send no endpoint retornado
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_employeeCreateQueueName}")).Result)
                .Verify(se => se.Send(It.Is<EmployeeCreateEvent>(dto => dto.Email == employeeCreateDto.Email && dto.Name == employeeCreateDto.Name), It.IsAny<CancellationToken>()), Times.Once);
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
                Password = "EmpSecurePassword123",
                ConfirmPassword = "EmpSecurePassword123"
            };
            var existingEmployee = new Employee { Id = 20, Email = employeeCreateDto.Email };

            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeCreateDto.Email)).ReturnsAsync(existingEmployee);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _employeeApplication.AddEmployee(employeeCreateDto));
            Assert.Equal("O email já existe cadastrado", exception.Message);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeCreateDto.Email), Times.Once);

            // Garante que GetSendEndpoint e Send não foram chamados
            _mockBus.Verify(b => b.GetSendEndpoint(It.IsAny<Uri>()), Times.Never);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_employeeCreateQueueName}")).Result)
                .Verify(se => se.Send(It.IsAny<EmployeeCreateDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddEmployee_ShouldThrowException_WhenBusSendFails()
        {
            // Arrange

            var expected = new EmployeeCreateEvent
            {
                TypeRole = TypeRole.Manager,
                Name = "Falha Publicacao",
                Email = "publishfailemployee@example.com",
                Password = "EmpSecurePassword123",                
            };

            var employeeCreateDto = new EmployeeCreateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Falha Publicacao",
                Email = "publishfailemployee@example.com",
                Password = "EmpSecurePassword123",
                ConfirmPassword = "EmpSecurePassword123"
            };

            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeCreateDto.Email)).ReturnsAsync((Employee)null);

            // Configura o mock de ISendEndpoint.Send para lançar uma exceção
            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeCreateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<EmployeeCreateEvent>(), It.IsAny<CancellationToken>()) == Task.FromException(new Exception("Simulated bus send error"))));

            _mapper.Setup(x => x.Map<EmployeeCreateEvent>(It.IsAny<EmployeeCreateDto>())).Returns(expected);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _employeeApplication.AddEmployee(employeeCreateDto));            
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeCreateDto.Email), Times.Once);

            // Garante que GetSendEndpoint e Send foram chamados (mesmo com erro)
            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeCreateQueueName}")), Times.Once);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_employeeCreateQueueName}")).Result)
                .Verify(se => se.Send(It.IsAny<EmployeeCreateEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

   

        [Fact]
        public async Task UpdateEmployee_ShouldReturnTrueAndSendToBus_WhenEmployeeExistsAndEmailNotChanged()
        {
            // Arrange
            int employeeId = 1;
            var expected = new EmployeeUpdateEvent
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Emp Name",
                Email = "originalemp@example.com"
            };

            var employeeUpdateDto = new EmployeeUpdateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Emp Name",
                Email = "originalemp@example.com"
            };
            var existingEmployee = new Employee { Id = employeeId, Email = "originalemp@example.com" };

            _mockEmployeeService.Setup(s => s.GetEmployeeById(employeeId)).ReturnsAsync(existingEmployee);
            // Email não mudou, então GetEmployeeByEmail não deve ser chamado para validação de existência

            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeUpdateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<EmployeeUpdateDto>(), It.IsAny<CancellationToken>()) == Task.CompletedTask));

            _mapper.Setup(x => x.Map<EmployeeUpdateEvent>(It.IsAny<EmployeeUpdateDto>())).Returns(expected);

            // Act
            var result = await _employeeApplication.UpdateEmployee(employeeId, employeeUpdateDto);

            // Assert
            Assert.True(result);
            _mockEmployeeService.Verify(s => s.GetEmployeeById(employeeId), Times.Once);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(It.IsAny<string>()), Times.Never);

            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeUpdateQueueName}")), Times.Once);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_employeeUpdateQueueName}")).Result)
                .Verify(se => se.Send(It.Is<EmployeeUpdateEvent>(dto =>
                    dto.Email == employeeUpdateDto.Email &&
                    dto.Name == employeeUpdateDto.Name &&
                    dto.TypeRole == employeeUpdateDto.TypeRole), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEmployee_ShouldReturnTrueAndSendToBus_WhenEmailChangedAndNewEmailIsUnique()
        {
            // Arrange
            int employeeId = 1;

            var expected = new EmployeeUpdateEvent
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Emp Name",
                Email = "newuniqueemp@example.com"
            };

            var employeeUpdateDto = new EmployeeUpdateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Emp Name",
                Email = "newuniqueemp@example.com"
            };
            var existingEmployee = new Employee { Id = employeeId, Email = "oldemp@example.com" }; // Email diferente

            _mockEmployeeService.Setup(s => s.GetEmployeeById(employeeId)).ReturnsAsync(existingEmployee);
            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeUpdateDto.Email)).ReturnsAsync((Employee)null); // Novo email é único

            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeUpdateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<EmployeeUpdateDto>(), It.IsAny<CancellationToken>()) == Task.CompletedTask));

            _mapper.Setup(x => x.Map<EmployeeUpdateEvent>(It.IsAny<EmployeeUpdateDto>())).Returns(expected);

            // Act
            var result = await _employeeApplication.UpdateEmployee(employeeId, employeeUpdateDto);

            // Assert
            Assert.True(result);
            _mockEmployeeService.Verify(s => s.GetEmployeeById(employeeId), Times.Once);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeUpdateDto.Email), Times.Once); // Deve verificar o novo email

            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeUpdateQueueName}")), Times.Once);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_employeeUpdateQueueName}")).Result)
                .Verify(se => se.Send(It.Is<EmployeeUpdateEvent>(dto =>
                    dto.Email == employeeUpdateDto.Email &&
                    dto.Name == employeeUpdateDto.Name &&
                    dto.TypeRole == employeeUpdateDto.TypeRole), It.IsAny<CancellationToken>()), Times.Once);
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
            _mockBus.Verify(b => b.GetSendEndpoint(It.IsAny<Uri>()), Times.Never);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_employeeUpdateQueueName}")).Result) // Use a fila correta aqui
                .Verify(se => se.Send(It.IsAny<EmployeeUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never);
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
            _mockBus.Verify(b => b.GetSendEndpoint(It.IsAny<Uri>()), Times.Never);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_employeeUpdateQueueName}")).Result) // Use a fila correta aqui
                .Verify(se => se.Send(It.IsAny<EmployeeUpdateEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEmployee_ShouldThrowException_WhenBusSendFails()
        {
            // Arrange
            int employeeId = 1;
            var expected = new EmployeeUpdateEvent
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Name",
                Email = "uniqueemp@example.com"
            };

            var employeeUpdateDto = new EmployeeUpdateDto
            {
                TypeRole = TypeRole.Manager,
                Name = "Updated Name",
                Email = "uniqueemp@example.com"
            };
            var existingEmployee = new Employee { Id = employeeId, Email = "originalemp@example.com" };

            _mockEmployeeService.Setup(s => s.GetEmployeeById(employeeId)).ReturnsAsync(existingEmployee);
            _mockEmployeeService.Setup(s => s.GetEmployeeByEmail(employeeUpdateDto.Email)).ReturnsAsync((Employee)null);

            // Configura o mock de ISendEndpoint.Send para lançar uma exceção
            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeUpdateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<EmployeeUpdateEvent>(), It.IsAny<CancellationToken>()) == Task.FromException(new Exception("Simulated bus send error"))));

            _mapper.Setup(x => x.Map<EmployeeUpdateEvent>(It.IsAny<EmployeeUpdateDto>())).Returns(expected);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _employeeApplication.UpdateEmployee(employeeId, employeeUpdateDto));
            
            _mockEmployeeService.Verify(s => s.GetEmployeeById(employeeId), Times.Once);
            _mockEmployeeService.Verify(s => s.GetEmployeeByEmail(employeeUpdateDto.Email), Times.Once);
            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_employeeUpdateQueueName}")), Times.Once);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_employeeUpdateQueueName}")).Result)
                .Verify(se => se.Send(It.IsAny<EmployeeUpdateEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}