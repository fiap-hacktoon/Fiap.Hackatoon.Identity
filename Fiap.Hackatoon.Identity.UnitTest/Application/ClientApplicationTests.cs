using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using MassTransit; // Ainda necessário para IBus (se outras classes o usarem)
using Microsoft.Extensions.Options; // Para mockar IOptions
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fiap.Hackatoon.Identity.UnitTest.Application
{
    public class ClientApplicationTests
    {
        private readonly Mock<IClientService> _mockClientService;
        private readonly Mock<ITokenApplication> _mockTokenApplication;
        private readonly Mock<IBusService> _mockBusService;
        private readonly Mock<IOptions<RabbitMqConnection>> _mockRabbitMqOptions;
        private readonly ClientApplication _clientApplication;

        public ClientApplicationTests()
        {
            _mockClientService = new Mock<IClientService>();
            _mockTokenApplication = new Mock<ITokenApplication>();
            _mockBusService = new Mock<IBusService>();
            _mockRabbitMqOptions = new Mock<IOptions<RabbitMqConnection>>();

            // Configura o mock do IOptions<RabbitMqConnection> para retornar sua configuração exata
            _mockRabbitMqOptions.Setup(o => o.Value).Returns(new RabbitMqConnection
            {
                HostName = "localhost", // Valores mockados, podem ser quaisquer strings válidas
                Port = "5672",
                UserName = "guest",
                Password = "guest",
                QueueNameClienteCreate = "fila_cliente_criado", // Usando o nome da sua propriedade
                QueueNameClienteUpdate = "fila_cliente_atualizado", // Usando o nome da sua propriedade
                QueueNameEmployeeCreate = "fila_funcionario_criado", // Usando o nome da sua propriedade
                QueueNameEmployeeUpdate = "fila_funcionario_atualizado" // Usando o nome da sua propriedade
            });

            _clientApplication = new ClientApplication(
                _mockClientService.Object,
                _mockTokenApplication.Object,
                _mockBusService.Object,
                _mockRabbitMqOptions.Object
            );
        }



        [Fact]
        public async Task Login_ShouldReturnToken_WhenClientIsValid()
        {
            // Arrange
            string search = "test@example.com";
            string password = "password123";
            var client = new Client { Id = 1, Email = search, Password = password, TypeRole = TypeRole.Client, Document = "12345678900", Birth = DateTime.Now.AddYears(-20) };
            string expectedToken = "mocked_jwt_token_for_valid_client";

            _mockClientService.Setup(s => s.GetClientLogin(search, password)).ReturnsAsync(client);
            _mockTokenApplication.Setup(t => t.GenerateToken(client)).Returns(expectedToken);

            // Act
            var result = await _clientApplication.Login(search, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedToken, result);
            _mockClientService.Verify(s => s.GetClientLogin(search, password), Times.Once);
            _mockTokenApplication.Verify(t => t.GenerateToken(client), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnNull_WhenClientIsInvalid()
        {
            // Arrange
            string search = "invalid@example.com";
            string password = "wrongpassword";

            _mockClientService.Setup(s => s.GetClientLogin(search, password)).ReturnsAsync((Client)null);

            // Act
            var result = await _clientApplication.Login(search, password);

            // Assert
            Assert.Null(result);
            _mockClientService.Verify(s => s.GetClientLogin(search, password), Times.Once);
            _mockTokenApplication.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Login_ShouldThrowException_WhenServiceThrowsException()
        {
            // Arrange
            string search = "test@example.com";
            string password = "password123";

            _mockClientService.Setup(s => s.GetClientLogin(search, password)).ThrowsAsync(new Exception("Database connection error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _clientApplication.Login(search, password));
            _mockClientService.Verify(s => s.GetClientLogin(search, password), Times.Once);
            _mockTokenApplication.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }
     

        [Fact]
        public async Task AddClient_ShouldReturnTrueAndSendToBus_WhenClientDoesNotExist()
        {
            // Arrange
            var clientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Novo Cliente",
                Email = "newclient@example.com",
                Document = "00011122233",
                Password = "SecurePassword123",
                ConfirmPassword = "SecurePassword123",
                Birth = new DateTime(1990, 5, 15)
            };

            _mockClientService.Setup(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document)).ReturnsAsync((Client)null);
            _mockBusService.Setup(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _clientApplication.AddClient(clientDto);

            // Assert
            Assert.True(result);
            _mockClientService.Verify(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document), Times.Once);
            // Verifica se SendToBus foi chamado com o DTO correto e o nome da fila correto
            _mockBusService.Verify(b => b.SendToBus(
                It.Is<ClientCreateDto>(dto =>
                    dto.Email == clientDto.Email &&
                    dto.Document == clientDto.Document),
                "fila_cliente_criado"), // Usando o nome da fila da RabbitMqConnection
                Times.Once);
        }

        [Fact]
        public async Task AddClient_ShouldThrowException_WhenClientAlreadyExists()
        {
            // Arrange
            var clientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Cliente Existente",
                Email = "existing@example.com",
                Document = "11122233344",
                Password = "ExistingPassword",
                ConfirmPassword = "ExistingPassword",
                Birth = new DateTime(1980, 1, 1)
            };
            var existingClient = new Client { Id = 10, Email = clientDto.Email, Document = clientDto.Document };

            _mockClientService.Setup(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document)).ReturnsAsync(existingClient);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _clientApplication.AddClient(clientDto));
            Assert.Equal("O email/document já existe cadastrado", exception.Message);
            _mockClientService.Verify(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document), Times.Once);
            _mockBusService.Verify(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AddClient_ShouldThrowException_WhenBusServiceSendFails()
        {
            // Arrange
            var clientDto = new ClientCreateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Falha Publicacao",
                Email = "publishfail@example.com",
                Document = "99988877766",
                Password = "PublishFailPassword",
                ConfirmPassword = "PublishFailPassword",
                Birth = new DateTime(1995, 10, 20)
            };

            _mockClientService.Setup(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document)).ReturnsAsync((Client)null);
            _mockBusService.Setup(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>())).ThrowsAsync(new Exception("BusService send error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _clientApplication.AddClient(clientDto));
            _mockClientService.Verify(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document), Times.Once);
            _mockBusService.Verify(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateClient_ShouldReturnTrueAndSendToBus_WhenClientExistsAndEmailNotChanged()
        {
            // Arrange
            int clientId = 1;
            var clientUpdateDto = new ClientUpdateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Updated Name",
                Email = "original@example.com",
                Document = "12345678900",
                Birth = new DateTime(1990, 1, 1)
            };
            var existingClient = new Client { Id = clientId, Email = "original@example.com", Document = "12345678900" };

            _mockClientService.Setup(s => s.GetClientById(clientId)).ReturnsAsync(existingClient);
            _mockBusService.Setup(b => b.SendToBus(It.IsAny<ClientUpdateDto>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _clientApplication.UpdateClient(clientId, clientUpdateDto);

            // Assert
            Assert.True(result);
            _mockClientService.Verify(s => s.GetClientById(clientId), Times.Once);
            _mockClientService.Verify(s => s.GetClientByEmail(It.IsAny<string>()), Times.Never);
            _mockBusService.Verify(b => b.SendToBus(
                It.Is<ClientUpdateDto>(dto =>
                    dto.Email == clientUpdateDto.Email &&
                    dto.Document == clientUpdateDto.Document),
                "fila_cliente_atualizado"), // Usando o nome da fila da RabbitMqConnection
                Times.Once);
        }

        [Fact]
        public async Task UpdateClient_ShouldReturnTrueAndSendToBus_WhenClientExistsAndEmailChangedAndNewEmailIsUnique()
        {
            // Arrange
            int clientId = 1;
            var clientUpdateDto = new ClientUpdateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Updated Name",
                Email = "newunique@example.com",
                Document = "12345678900",
                Birth = new DateTime(1990, 1, 1)
            };
            var existingClient = new Client { Id = clientId, Email = "old@example.com", Document = "12345678900" };

            _mockClientService.Setup(s => s.GetClientById(clientId)).ReturnsAsync(existingClient);            
            _mockBusService.Setup(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _clientApplication.UpdateClient(clientId, clientUpdateDto);

            // Assert
            Assert.True(result);
            _mockClientService.Verify(s => s.GetClientById(clientId), Times.Once);         
            _mockBusService.Verify(b => b.SendToBus(
                It.Is<ClientUpdateDto>(dto =>
                    dto.Email == clientUpdateDto.Email &&
                    dto.Document == clientUpdateDto.Document),
                "fila_cliente_atualizado"), // Usando o nome da fila da RabbitMqConnection
                Times.Once);
        }

        [Fact]
        public async Task UpdateClient_ShouldThrowException_WhenClientNotFound()
        {
            // Arrange
            int clientId = 999;
            var clientUpdateDto = new ClientUpdateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Name",
                Email = "email@example.com",
                Document = "123",
                Birth = DateTime.Now
            };

            _mockClientService.Setup(s => s.GetClientById(clientId)).ReturnsAsync((Client)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _clientApplication.UpdateClient(clientId, clientUpdateDto));
            // Ajuste a mensagem de erro para "Client" se o seu código original usar "Client" em vez de "Employee"
            Assert.Equal($"Client com id:{clientId} não encontrado", exception.Message);
            _mockClientService.Verify(s => s.GetClientById(clientId), Times.Once);
            _mockClientService.Verify(s => s.GetClientByEmail(It.IsAny<string>()), Times.Never);
            _mockBusService.Verify(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>()), Times.Never);
        }

        

        [Fact]
        public async Task UpdateClient_ShouldThrowException_WhenBusServiceSendFails()
        {
            // Arrange
            int clientId = 1;
            var clientUpdateDto = new ClientUpdateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Updated Name",
                Email = "unique@example.com",
                Document = "12345678900",
                Birth = new DateTime(1990, 1, 1)
            };
            var existingClient = new Client { Id = clientId, Email = "original@example.com", Document = "12345678900" };

            _mockClientService.Setup(s => s.GetClientById(clientId)).ReturnsAsync(existingClient);
            
            _mockBusService.Setup(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>())).ThrowsAsync(new Exception("BusService update send error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _clientApplication.UpdateClient(clientId, clientUpdateDto));
            Assert.Equal("BusService update send error", exception.Message);
            _mockClientService.Verify(s => s.GetClientById(clientId), Times.Once);
            
            _mockBusService.Verify(b => b.SendToBus(It.IsAny<EventDto>(), It.IsAny<string>()), Times.Once);
        }
    }
}