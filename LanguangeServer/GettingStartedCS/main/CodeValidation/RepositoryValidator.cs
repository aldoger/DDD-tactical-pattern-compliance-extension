using GettingStartedCS.main.IdentifyDomainModel;
using GettingStartedCS.main.StructureInfo;
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
            bool c12 = ValidateConstrainC12(repository, domainList);
            bool c13 = ValidateConstraintC13(repository);
            bool c14 = ValidateConstraintC14(repository, domainList);
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
        public bool ValidateConstrainC12(StructureInfo.Repository repository, DomainModelList domainList)
        {
            foreach(var method in repository.ClassInfo.Methods)
            {
                foreach (var parameter in method.Parameters)
                {
                    var domainModel = domainList.GetDomainModelFirst(parameter.ParameterType);
                    if(domainModel == null)
                    {
                        return false;
                    }
                    if(domainModel is not Entity && domainModel is not ValueObject && domainModel is not Aggregate)
                    {
                        return false;
                    }
                }
            }
            return true;   
        }
        public bool ValidateConstraintC13(StructureInfo.Repository repository)
        {
            if(repository == null)
            {
                return false;
            }
            return repository.ClassInfo.Properties.Count == 0;
        }
        public bool ValidateConstraintC14(StructureInfo.Repository repository, DomainModelList domainList)
        {
            var domainModels = domainList.GetDomainModel(repository.Name);
            if (domainModels == null || domainModels.Count == 0)
            {
                return false;
            }
            return true;
        }
    }
}
