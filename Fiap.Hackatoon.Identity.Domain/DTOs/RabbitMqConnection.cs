using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.DTOs
{
    public class RabbitMqConnection
    {
        public string HostName { get; set; }
        public string Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string QueueNameClienteCreate { get; set; }
        public string QueueNameClienteUpdate { get; set; }
        public string QueueNameEmployeeCreate { get; set; }
        public string QueueNameEmployeeUpdate { get; set; }
    }
}


