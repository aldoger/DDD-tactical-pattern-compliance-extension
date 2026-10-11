using GettingStartedCS.main.StructureInfo;
using System;
using GettingStartedCS.parser;

namespace GettingStartedCS.main.CodeValidation
{
    public class ValidatorFactory
    {
        public static DomainObjectValidate GetValidator(DomainModelStruct domainType, Parser par)
        {
            switch (domainType)
            {
                case Entity:
                    return new EntityValidator(par);
                case ValueObject:
                    return new ValueObjectValidator(par);
                case DomainEvent:
                    return new DomainEventValidator(par);
                case DomainService:
                    return new DomainServiceValidator(par);
                case Repository:
                    return new RepositoryValidator(par);
                case Factory:
                    return new FactoryValidator(par);
                case Aggregate:
                    return new AggregateValidator(par);
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
