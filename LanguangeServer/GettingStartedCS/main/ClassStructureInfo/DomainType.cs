using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.ClassStructureInfo
{
    public class DomainType
    {
        public Dictionary<string, string> Domains { get; } = new Dictionary<string, string>
        {
            { "ENTITY", "Entity" },
            { "VALUE_OBJECT", "Value Object" },
            { "AGGREGATE", "Aggregate" },
            { "REPOSITORY", "Repository" },
            { "DOMAIN_SERVICE", "Domain Service" },
            { "DOMAIN_EVENT", "Domain Event" },
            { "DOMAIN_EVENT", "Domain Event" },
            { "FACTORY", "Factory" }
        };

        public DomainType()
        {
        }

        public bool IsValidDomain(string domain)
        {
            return Domains.ContainsKey(domain);
        }

        public string GetDomainDescription(string domain)
        {
            if (IsValidDomain(domain))
            {
                return Domains[domain];
            }
            else
            {
                throw new ArgumentException($"Invalid domain: {domain}");
            }
        }
    }
}
