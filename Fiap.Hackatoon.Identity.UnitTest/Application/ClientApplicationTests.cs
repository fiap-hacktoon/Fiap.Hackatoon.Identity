using Moq;
using Xunit;
using Fiap.Hackatoon.Identity.Application.Applications;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Enumerators;
using MassTransit; // Para IBus
using Microsoft.Extensions.Options; // Para IOptions
using System.Threading.Tasks;
using System;
using System.Net.Sockets;
using Fiap.Hackatoon.Shared.Dto; // Para Uri, pode ser necessário para GetSendEndpoint mock

namespace Fiap.Hackatoon.Identity.UnitTest.Application
{
    public class ClientApplicationTests
    {
        private readonly Mock<IClientService> _mockClientService;
        private readonly Mock<ITokenApplication> _mockTokenApplication;
        private readonly Mock<IBus> _mockBus; // Mock do IBus (direto)
        private readonly Mock<IOptions<RabbitMqConnection>> _mockRabbitMqOptions;
        private readonly ClientApplication _clientApplication;

        // Nomes de fila que serão configurados no mock de RabbitMqConnection
        private readonly string _clientCreateQueueName = "fila_cliente_criado";
        private readonly string _clientUpdateQueueName = "fila_cliente_atualizado";

        public ClientApplicationTests()
        {
            _mockClientService = new Mock<IClientService>();
            _mockTokenApplication = new Mock<ITokenApplication>();
            _mockBus = new Mock<IBus>(); // Instancia o mock para IBus
            _mockRabbitMqOptions = new Mock<IOptions<RabbitMqConnection>>();

            // Configura o mock do IOptions<RabbitMqConnection>
            _mockRabbitMqOptions.Setup(o => o.Value).Returns(new RabbitMqConnection
            {
                QueueNameClienteCreate = _clientCreateQueueName,
                QueueNameClienteUpdate = _clientUpdateQueueName,
                // Outras propriedades podem ser configuradas se usadas no código
                HostName = "localhost",
                Port = "5672",
                UserName = "guest",
                Password = "guest"
            });

            _clientApplication = new ClientApplication(
                _mockClientService.Object,
                _mockTokenApplication.Object,
                _mockBus.Object, // Passa o mock de IBus
                _mockRabbitMqOptions.Object
            );

            // Setup inicial para o GetSendEndpoint do IBus.
            // Para cada chamada Send, MassTransit.IBus.GetSendEndpoint é chamado primeiro.
            // Precisamos mockar isso para retornar um IEndpoint mockado.
            _mockBus.Setup(b => b.GetSendEndpoint(It.IsAny<Uri>()))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>()); // Retorna um mock de ISendEndpoint
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

            // Certifique-se que o mock de ISendEndpoint.Send também está configurado
            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientCreateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<ClientCreateDto>(), It.IsAny<CancellationToken>()) == Task.CompletedTask));

            // Act
            var result = await _clientApplication.AddClient(clientDto);

            // Assert
            Assert.True(result);
            _mockClientService.Verify(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document), Times.Once);

            // Verifica a chamada a GetSendEndpoint com a URI correta
            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientCreateQueueName}")), Times.Once);
            // Verifica a chamada a Send no endpoint retornado
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_clientCreateQueueName}")).Result)
                .Verify(se => se.Send(It.Is<ClientCreateDto>(dto => dto.Email == clientDto.Email && dto.Document == clientDto.Document), It.IsAny<CancellationToken>()), Times.Once);
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

            // Garante que GetSendEndpoint e Send não foram chamados
            _mockBus.Verify(b => b.GetSendEndpoint(It.IsAny<Uri>()), Times.Never);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_clientCreateQueueName}")).Result)
                .Verify(se => se.Send(It.IsAny<ClientCreateDto>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddClient_ShouldThrowException_WhenBusSendFails()
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

            // Configura o mock de ISendEndpoint.Send para lançar uma exceção
            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientCreateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<ClientCreateDto>(), It.IsAny<CancellationToken>()) == Task.FromException(new Exception("Simulated bus send error"))));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _clientApplication.AddClient(clientDto));
            _mockClientService.Verify(s => s.GetClientByEmailOrDocument(clientDto.Email, clientDto.Document), Times.Once);

            // Garante que GetSendEndpoint e Send foram chamados (mesmo com erro)
            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientCreateQueueName}")), Times.Once);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_clientCreateQueueName}")).Result)
                .Verify(se => se.Send(It.IsAny<ClientCreateDto>(), It.IsAny<CancellationToken>()), Times.Once);
        }



        [Fact]
        public async Task UpdateClient_ShouldReturnTrueAndSendToBus_WhenClientExistsAndEmailAndDocumentNotChanged()
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
            // Email e Documento não mudaram, então GetClientByEmail/Document não devem ser chamados para validação de existência

            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientUpdateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<ClientUpdateDto>(), It.IsAny<CancellationToken>()) == Task.CompletedTask));

            // Act
            var result = await _clientApplication.UpdateClient(clientId, clientUpdateDto);

            // Assert
            Assert.True(result);
            _mockClientService.Verify(s => s.GetClientById(clientId), Times.Once);
            _mockClientService.Verify(s => s.GetClientByEmail(It.IsAny<string>()), Times.Never);
            _mockClientService.Verify(s => s.GetClientByDocument(It.IsAny<string>()), Times.Never);

            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientUpdateQueueName}")), Times.Once);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_clientUpdateQueueName}")).Result)
                .Verify(se => se.Send(It.Is<ClientUpdateDto>(dto =>
                    dto.Email == clientUpdateDto.Email &&
                    dto.Document == clientUpdateDto.Document), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateClient_ShouldReturnTrueAndSendToBus_WhenEmailChangedAndNewEmailIsUnique()
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
            var existingClient = new Client { Id = clientId, Email = "old@example.com", Document = "12345678900" }; // Email diferente

            _mockClientService.Setup(s => s.GetClientById(clientId)).ReturnsAsync(existingClient);
            _mockClientService.Setup(s => s.GetClientByEmail(clientUpdateDto.Email)).ReturnsAsync((Client)null); // Novo email é único
            _mockClientService.Setup(s => s.GetClientByDocument(clientUpdateDto.Document)).ReturnsAsync(existingClient); // Documento é o mesmo do cliente original

            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientUpdateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<ClientUpdateDto>(), It.IsAny<CancellationToken>()) == Task.CompletedTask));

            // Act
            var result = await _clientApplication.UpdateClient(clientId, clientUpdateDto);

            // Assert
            Assert.True(result);
            _mockClientService.Verify(s => s.GetClientById(clientId), Times.Once);
            _mockClientService.Verify(s => s.GetClientByEmail(clientUpdateDto.Email), Times.Once); // Deve verificar o novo email
            _mockClientService.Verify(s => s.GetClientByDocument(It.IsAny<string>()), Times.Never); // Documento não mudou

            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientUpdateQueueName}")), Times.Once);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_clientUpdateQueueName}")).Result)
                .Verify(se => se.Send(It.Is<ClientUpdateDto>(dto =>
                    dto.Email == clientUpdateDto.Email &&
                    dto.Document == clientUpdateDto.Document), It.IsAny<CancellationToken>()), Times.Once);
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

            _mockClientService.Setup(s => s.GetClientById(clientId)).ReturnsAsync((Client)null); // Cliente não encontrado

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _clientApplication.UpdateClient(clientId, clientUpdateDto));
            Assert.Equal($"Client com id:{clientId} não encontrado", exception.Message);
            _mockClientService.Verify(s => s.GetClientById(clientId), Times.Once);
            _mockClientService.Verify(s => s.GetClientByEmail(It.IsAny<string>()), Times.Never);
            _mockClientService.Verify(s => s.GetClientByDocument(It.IsAny<string>()), Times.Never);
            _mockBus.Verify(b => b.GetSendEndpoint(It.IsAny<Uri>()), Times.Never);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_clientCreateQueueName}")).Result)
                .Verify(se => se.Send(It.IsAny<ClientUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never); // Verificação para Send
        }

        [Fact]
        public async Task UpdateClient_ShouldThrowException_WhenNewEmailAlreadyUsedByAnotherClient()
        {
            // Arrange
            int clientId = 1;
            var clientUpdateDto = new ClientUpdateDto
            {
                TypeRole = TypeRole.Client,
                Name = "Updated Name",
                Email = "alreadyused@example.com",
                Document = "12345678900",
                Birth = new DateTime(1990, 1, 1)
            };
            var existingClient = new Client { Id = clientId, Email = "original@example.com", Document = "12345678900" };
            var clientWithExistingEmail = new Client { Id = 2, Email = "alreadyused@example.com" }; // Outro cliente usando o email

            _mockClientService.Setup(s => s.GetClientById(clientId)).ReturnsAsync(existingClient);
            _mockClientService.Setup(s => s.GetClientByEmail(clientUpdateDto.Email)).ReturnsAsync(clientWithExistingEmail); // Email já está em uso por outro cliente

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _clientApplication.UpdateClient(clientId, clientUpdateDto));
            Assert.Equal($"O email {clientUpdateDto.Email} já está sendo usado para outro cliente", exception.Message);
            _mockClientService.Verify(s => s.GetClientById(clientId), Times.Once);
            _mockClientService.Verify(s => s.GetClientByEmail(clientUpdateDto.Email), Times.Once);
            _mockClientService.Verify(s => s.GetClientByDocument(It.IsAny<string>()), Times.Never); // Documento não mudou
            _mockBus.Verify(b => b.GetSendEndpoint(It.IsAny<Uri>()), Times.Never);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_clientCreateQueueName}")).Result)
                .Verify(se => se.Send(It.IsAny<ClientUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never); // Verificação para Send
        }
      


        [Fact]
        public async Task UpdateClient_ShouldThrowException_WhenBusSendFails()
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
            _mockClientService.Setup(s => s.GetClientByEmail(clientUpdateDto.Email)).ReturnsAsync((Client)null); // Email único
            _mockClientService.Setup(s => s.GetClientByDocument(clientUpdateDto.Document)).ReturnsAsync(existingClient); // Documento não mudou

            // Configura o mock de ISendEndpoint.Send para lançar uma exceção
            _mockBus.Setup(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientUpdateQueueName}")))
                     .ReturnsAsync(Mock.Of<ISendEndpoint>(se => se.Send(It.IsAny<ClientUpdateDto>(), It.IsAny<CancellationToken>()) == Task.FromException(new Exception("Simulated bus send error"))));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _clientApplication.UpdateClient(clientId, clientUpdateDto));            
            _mockClientService.Verify(s => s.GetClientById(clientId), Times.Once);
            _mockClientService.Verify(s => s.GetClientByEmail(clientUpdateDto.Email), Times.Once);
            _mockClientService.Verify(s => s.GetClientByDocument(It.IsAny<string>()), Times.Never); // Documento não mudou

            _mockBus.Verify(b => b.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == $"queue:{_clientUpdateQueueName}")), Times.Once);
            Mock.Get(_mockBus.Object.GetSendEndpoint(new Uri($"queue:{_clientUpdateQueueName}")).Result)
                .Verify(se => se.Send(It.IsAny<ClientUpdateDto>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}