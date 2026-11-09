using DomainTacticalPatternCompliance.main.Identifier;
using DomainTacticalPatternCompliance.main.StructureInfo;
using DomainTacticalPatternCompliance.parser;

namespace DomainTacticalPatternCompliance.main.CodeValidation
{
    public abstract class DomainObjectValidate
    {
        protected Parser parser;

        public DomainObjectValidate(Parser par)
        {
            parser = par;
        }

        public abstract void Validate(
            DomainModelStruct classInfo,
            ViolationList vlist,
            DomainModelList domainList
        );
    }
}
