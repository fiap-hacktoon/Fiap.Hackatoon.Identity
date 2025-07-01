using Fiap.Hackatoon.Identity.Domain.Entities;
using Fiap.Hackatoon.Identity.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.Services
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;    


        public UserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? CurrentUser => _httpContextAccessor.HttpContext?.User;

        public string? GetRole()
        {
            return CurrentUser?.FindFirst(ClaimTypes.Role)?.Value;
        }        

        public string? GetUserEmail()
        {
            return CurrentUser?.FindFirst(ClaimTypes.Email)?.Value;
        }

        public string? GetUserId()
        {
            return CurrentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
