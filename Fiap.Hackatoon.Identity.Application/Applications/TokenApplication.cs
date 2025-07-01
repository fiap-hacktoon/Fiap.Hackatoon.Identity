using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Applications;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;



namespace Fiap.Hackatoon.Identity.Application.Applications
{
    public class TokenApplication(IConfiguration configuration) : ITokenApplication
    {
        private readonly IConfiguration _configuration = configuration;
        public string GenerateToken(User user)
        {
            try
            {
                if (user is null) throw new Exception("Usuáio não encontrado");

                var tokeHandler = new JwtSecurityTokenHandler();                
                var chaveCriptografia = Encoding.ASCII.GetBytes(_configuration.GetValue<string>("SecretJWT") ?? string.Empty);

                var tokenPropriedades = new SecurityTokenDescriptor()
                {
                    Subject = new System.Security.Claims.ClaimsIdentity(new[]
                    {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString() ?? string.Empty),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Email ?? string.Empty),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.TypeRole.ToString())

                }),

                    //tempo de expiração do token
                    Expires = DateTime.UtcNow.AddHours(1),

                    //chave de criptografia
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(chaveCriptografia),
                                                                SecurityAlgorithms.HmacSha256Signature)
                };

                //cria o token
                var token = tokeHandler.CreateToken(tokenPropriedades);

                //retorna o token criado
                return tokeHandler.WriteToken(token);                
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
    }
}
