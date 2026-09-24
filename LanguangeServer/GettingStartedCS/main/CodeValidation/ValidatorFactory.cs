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
