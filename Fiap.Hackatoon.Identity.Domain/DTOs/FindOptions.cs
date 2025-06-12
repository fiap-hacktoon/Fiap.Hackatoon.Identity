using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiap.Hackatoon.Identity.Domain.DTOs
{
    public class FindOptions
    {
        public bool IsIgnoreAutoIncludes { get; set; }
        public bool IsAsNoTracking { get; set; }
    }
}
