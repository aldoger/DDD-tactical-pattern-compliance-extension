using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class RepositoryValidator : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo.ClassStructureInfo classInfo, ViolationList vlist)
        {
            throw new NotImplementedException();
        }
        public bool ValidateConstrainC12(ClassStructureInfo.ClassStructureInfo repository)
        {
            // TODO: Access method parameter and get the parameter type
            return true;   
        }
        public bool ValidateConstraintC13(ClassStructureInfo.ClassStructureInfo repository)
        {
            if(repository.Properties.Count == 0)
            {
                return true;
            }
            return false;
        }
        public bool ValidateConstraintC14(ClassStructureInfo.ClassStructureInfo repository)
        {
            // Still confused C14
            return true;
        }
    }
}
