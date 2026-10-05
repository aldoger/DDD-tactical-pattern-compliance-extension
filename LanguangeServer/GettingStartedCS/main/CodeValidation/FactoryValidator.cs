using GettingStartedCS.main.IdentifyDomainModel;
using GettingStartedCS.main.StructureInfo;

namespace GettingStartedCS.main.CodeValidation
{
    public class FactoryValidator : DomainObjectValidate
    {
        public override void Validate(StructureInfo.DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            StructureInfo.Factory factory = (StructureInfo.Factory)domainModel;
            bool c15 = ValidateConstraintC15(factory, domainList);
            bool c16 = ValidateConstraintC16(factory);
            if (!c15)
            {
                vlist.AddViolation(new Violation(
                    factory.Name, 
                    Constraint.CONSTRAINT_15,
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_15))
                 );
            }
            if (!c16)
            {
                vlist.AddViolation(new Violation(
                    factory.Name, 
                    Constraint.CONSTRAINT_16, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_16))
                );
            }
        }
        public bool ValidateConstraintC15(StructureInfo.Factory factory, DomainModelList domainList)
        {
            // TODO: Access the return type of the factory method and check if it is a class type
            var factoryClassInfo = factory.ClassInfo;
            if (factoryClassInfo == null)
            {
                return false;
            }
            foreach(var method in factoryClassInfo.Methods)
            {
                var returnType = method.ReturnType;
                DomainModelStruct? domainModel = domainList.GetDomainModel(returnType);
                if (domainModel == null)
                {
                    return false;
                }
                // Aggregate maybe must be a root not part
                if(domainModel is not Entity && domainModel is not ValueObject && domainModel is not Aggregate)
                {
                    return false;
                }
            }
            return true;
        }
        public bool ValidateConstraintC16(StructureInfo.Factory factory)
        {
            // Still confused
            return true;
        }
    }
}
