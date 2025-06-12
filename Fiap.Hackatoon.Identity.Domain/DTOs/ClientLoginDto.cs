using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.DTOs
{
    public class ClientLoginDto
    {
        [Required(ErrorMessage = "Email/CPF obrigatório")]
        public string EmailOrDocument { get; set; }

        [Required(ErrorMessage = "Senha é obrigatório")]
        public string Password { get; set; }
    }
}
