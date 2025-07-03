using Fiap.Hackatoon.Identity.Domain.Enumerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Shared.Dto
{
    public class  ClientUpdateDto
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
        public DateOnly Birth { get; set; }
    }
}
