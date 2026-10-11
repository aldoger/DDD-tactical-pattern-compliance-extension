using GettingStartedCS.main.Identifier;
using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.parser;

namespace GettingStartedCS.main.CodeValidation
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
