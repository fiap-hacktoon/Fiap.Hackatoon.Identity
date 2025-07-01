using Fiap.Hackatoon.Identity.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Interfaces.Services
{
    public interface IBusService
    {
        Task SendToBus(EventDto employeeCreate, string queueName);
    }
}
