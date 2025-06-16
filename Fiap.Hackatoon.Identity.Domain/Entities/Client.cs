namespace Fiap.Hackatoon.Identity.Domain.Entities
{
    public class Client : User
    {
        public string Document  { get; set; }

        public DateTime Birth { get; set; }
    }
}
