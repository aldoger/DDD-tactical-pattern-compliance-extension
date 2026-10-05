using GettingStartedCS.main.StructureInfo;
using System;

namespace GettingStartedCS.main.CodeValidation
{
    public class ValidatorFactory
    {
        public static DomainObjectValidate GetValidator(DomainModelStruct domainType)
        {
            switch (domainType)
            {
                case Entity:
                    return new EntityValidate();
                case ValueObject:
                    return new ValueObjectValidator();
                case DomainEvent:
                    return new DomainEventValidator();
                case DomainService:
                    return new DomainServiceValidator();
                case Repository:
                    return new RepositoryValidator();
                case Factory:
                    return new FactoryValidator();
                case Aggregate:
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
