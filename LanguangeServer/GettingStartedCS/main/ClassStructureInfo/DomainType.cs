using System;

namespace GettingStartedCS.main.ClassStructureInfo
{
    public enum DomainType
    {
        ENTITY,
        VALUE_OBJECT,
        AGGREGATE,
        REPOSITORY,
        DOMAIN_SERVICE,
        DOMAIN_EVENT,
        FACTORY
    }

    public static class DomainTypeExtensions
    {
        public static string GetDisplayName(this DomainType domainType)
        {
            switch (domainType)
            {
                case DomainType.ENTITY:
                    return "Entity";

                case DomainType.VALUE_OBJECT:
                    return "Value Object";

                case DomainType.AGGREGATE:
                    return "Aggregate";

                case DomainType.REPOSITORY:
                    return "Repository";

                case DomainType.DOMAIN_SERVICE:
                    return "Domain Service";

                case DomainType.DOMAIN_EVENT:
                    return "Domain Event";

                case DomainType.FACTORY:
                    return "Factory";

                default:
                    throw new ArgumentOutOfRangeException(nameof(domainType));
            }
        }
    }
}