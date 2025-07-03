using Fiap.Hackatoon.Identity.Domain.Enumerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Shared.Dto
{
    public class EmployeeUpdateEvent
    {
        public int Id { get; set; }
        public TypeRole TypeRole { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }        
    }
}
