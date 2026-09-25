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
