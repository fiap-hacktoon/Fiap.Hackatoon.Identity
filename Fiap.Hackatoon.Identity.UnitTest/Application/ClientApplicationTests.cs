using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using MassTransit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.UnitTest.Application
{

    public class ClientApplicationTests
    {
        private readonly Mock<IClientService> _mockClientService;
        private readonly Mock<ITokenApplication> _mockTokenApplication;
        private readonly Mock<IBusService> _mockBusService;
        private readonly Mock<IBus> _mockBus;
        private readonly ClientApplication _clientApplication;

        public ClientApplicationTests()
        {
            _mockClientService = new Mock<IClientService>();
            _mockTokenApplication = new Mock<ITokenApplication>();
            _mockBusService = new Mock<IBusService>();
            _mockBus = new Mock<IBus>();
            _clientApplication = new ClientApplication(
                _mockClientService.Object,
                _mockTokenApplication.Object,               
                _mockBusService.Object
            );
        }

        [Fact]
        public async Task Login_ShouldReturnToken_WhenClientIsValid()
        {
            // Arrange
            string search = "test@example.com";
            string password = "password123";
            // Usamos a entidade Client, que herda de User
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

            _mockClientService.Setup(s => s.GetClientLogin(search, password)).ReturnsAsync((Client)null); // Retorna null

            // Act
            var result = await _clientApplication.Login(search, password);

            // Assert
            Assert.Null(result);
            _mockClientService.Verify(s => s.GetClientLogin(search, password), Times.Once);
            _mockTokenApplication.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never); // Nenhuma tentativa de gerar token
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
        public async Task AddClient_ShouldReturnTrueAndPublishMessage_WhenClientDoesNotExist()
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
            _mockBus.Setup(b => b.Publish(It.IsAny<ClientCreateDto>(), default)).Returns(Task.CompletedTask);

            // Act
            var result = await _clientApplication.AddClient(clientDto);

            // Assert
            Assert.True(result);
            _mockClientService.Verify(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document), Times.Once);
            // Verifica se o método Publish foi chamado com o DTO correto
            _mockBus.Verify(b => b.Publish(It.Is<ClientCreateDto>(dto =>
                dto.Email == clientDto.Email && dto.Document == clientDto.Document), default), Times.Once);
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
            // Retorna um cliente existente para simular o cenário de duplicidade
            var existingClient = new Client { Id = 10, Email = clientDto.Email, Document = clientDto.Document };

            _mockClientService.Setup(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document)).ReturnsAsync(existingClient);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _clientApplication.AddClient(clientDto));
            Assert.Equal("O email/document já existe cadastrado", exception.Message);
            _mockClientService.Verify(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document), Times.Once);
            _mockBus.Verify(b => b.Publish(It.IsAny<ClientCreateDto>(), default), Times.Never); // Não deve publicar se o cliente já existe
        }

        [Fact]
        public async Task AddClient_ShouldThrowException_WhenBusPublishFails()
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
            _mockBus.Setup(b => b.Publish(It.IsAny<ClientCreateDto>(), default)).ThrowsAsync(new Exception("MassTransit publish simulation error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _clientApplication.AddClient(clientDto));
            _mockClientService.Verify(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document), Times.Once);
            _mockBus.Verify(b => b.Publish(It.IsAny<ClientCreateDto>(), default), Times.Once); // Verifica se a tentativa de publicação ocorreu
        }
    }
}

