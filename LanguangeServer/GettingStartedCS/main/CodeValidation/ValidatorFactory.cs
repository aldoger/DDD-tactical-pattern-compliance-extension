using GettingStartedCS.main.ClassStructureInfo;
using System;

namespace GettingStartedCS.main.CodeValidation
{
    public class ValidatorFactory
    {
        public static DomainObjectValidate GetValidator(DomainType domainType)
        {
            switch (domainType)
            {
                case DomainType.ENTITY:
                    return new EntityValidate();
                case DomainType.VALUE_OBJECT:
                    return new ValueObjectValidator();
                case DomainType.DOMAIN_EVENT:
                    return new DomainEventValidator();
                case DomainType.DOMAIN_SERVICE:
                    return new DomainServiceValidator();
                case DomainType.REPOSITORY:
                    return new RepositoryValidator();
                case DomainType.FACTORY:
                    return new FactoryValidator();
                case DomainType.AGGREGATE_PART or DomainType.AGGREGATE_ROOT:
                    return new AggregateValidator();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(domainType),
                        domainType,
                        null
                    );
            }
        }
    }
}
