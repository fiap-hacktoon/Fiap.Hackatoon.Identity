using Fiap.Hackatoon.Identity.Domain.Enumerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.DTOs
{
    public class ClientCreateDto
    {   
        [Required(ErrorMessage = "O campo role é obrigatório")]
        public TypeRole TypeRole { get; set; }

        [Required(ErrorMessage = "O campo nome é obrigatório")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O campo e-mail é obrigatório")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo document é obrigatório")]
        public string Document { get; set; }

        [Required(ErrorMessage = "Digite a senha")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Digite a confirmação da senha")]        
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "O campo Birth é obrigatório")]
        public DateTime Birth { get; set; }        
    }
}
