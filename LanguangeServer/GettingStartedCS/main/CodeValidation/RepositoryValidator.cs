using GettingStartedCS.main.IdentifyDomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class RepositoryValidator : DomainObjectValidate
    {
        public override void Validate(StructureInfo.DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            StructureInfo.Repository repository = (StructureInfo.Repository)domainModel;
            bool c12 = ValidateConstrainC12(repository);
            bool c13 = ValidateConstraintC13(repository);
            bool c14 = ValidateConstraintC14(repository );
            if (!c12)
            {
                vlist.AddViolation(new Violation(
                    repository.Name, 
                    Constraint.CONSTRAINT_12, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_12))
                );
            }
            if (!c13)
            {
                vlist.AddViolation(new Violation(
                    repository.Name, 
                    Constraint.CONSTRAINT_13, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_13))
                );
            }
            if (!c14)
            {
                vlist.AddViolation(new Violation(
                    repository.Name, 
                    Constraint.CONSTRAINT_14, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_14))
                );
            }
        }
        public bool ValidateConstrainC12(StructureInfo.Repository repository)
        {
            // TODO: Access method parameter and get the parameter type
            return true;   
        }
        public bool ValidateConstraintC13(StructureInfo.Repository repository)
        {
            return repository.HasProperties;
        }
        public bool ValidateConstraintC14(StructureInfo.Repository repository)
        {
            // Still confused C14
            return true;
        }
    }
}
