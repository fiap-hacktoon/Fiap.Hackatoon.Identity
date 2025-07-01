using Fiap.Hackatoon.Identity.Domain.DTOs;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Services
{
    public class BusService:IBusService
    {
        private readonly IBus _bus;

        public BusService(IBus bus)
        {
            _bus = bus;
        }

        public async Task SendToBus(EventDto employeeCreate,string queueName)
        {
            var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{queueName}"));

            await endpoint.Send(employeeCreate);
        }
    }

}
