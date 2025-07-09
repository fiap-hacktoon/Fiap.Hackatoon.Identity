using Fiap.Hackatoon.Identity.Domain.Interfaces.Elastic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Infrastructure.ElasticSearch
{
    public class ElasticSettings : IElasticSettings
    {
        public string ApiKey { get; set; }

        public string CloudId { get; set; }
    }
}
