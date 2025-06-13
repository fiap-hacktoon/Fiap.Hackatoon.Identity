using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.DTOs
{
    public class EmployeeLoginDto
    {
        [Required(ErrorMessage ="Email é obrigatório")]        
        public string Email { get; set; }

        [Required(ErrorMessage = "Senha é obrigatório")]
        public string Password { get; set; }
    }
}
