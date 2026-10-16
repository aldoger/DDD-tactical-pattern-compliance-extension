using GettingStartedCS.main.Identifier;
using GettingStartedCS.main.StructureInfo;
using System;
using GettingStartedCS.parser;
using GettingStartedCS.Util;

namespace GettingStartedCS.main.CodeValidation
{
    public class FactoryValidator : DomainObjectValidate
    {
        public FactoryValidator(Parser par) : base(par)
        { }
        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            Factory fc = (Factory)domainModel;
            bool c14 = ValidateConstraintC14(fc, domainList);
            bool c15 = ValidateConstraintC15(fc, domainList);

            string name = "";
            if (fc.IsMethod) name = fc.MethodInfo.MethodName;
            else if (fc.IsClass) name = fc.ClassInfo.ClassName;

            if(!c14)
            {
                vlist.AddViolation(
                    new Violation(
                        name,
                        Constraint.CONSTRAINT_C14,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C14)
                    )
                );
            }

            if (!c15)
            {
                vlist.AddViolation(
                    new Violation(
                        name,
                        Constraint.CONSTRAINT_C15,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C15)
                    )
                );
            }
        }

        private bool ValidateConstraintC14(Factory fc, DomainModelList domainList)
        {
            if (fc.ClassInfo == null) return false;
            var domainModels = domainList.GetDomainModel(fc.ClassInfo.ClassName);
            if (domainModels == null || domainModels.Count > 1) return false;
            return true;
        }

        private bool ValidateConstraintC15(Factory fc, DomainModelList domainList)
        {
            if (fc.IsMethod && fc.MethodInfo != null)
            {
                var returnType = fc.MethodInfo.ReturnType;
                var domainModel = domainList.GetDomainModelFirst(returnType);
                if (domainModel == null) return false;
                if (Util.Util.IsDomainModelEntityOrValueObjectOrAggregate(domainModel)) return true;
                return false;
            }

            if(fc.IsClass && fc.ClassInfo != null)
            {
                foreach(var method in fc.ClassInfo.Methods)
                {
                    var returnType = method.ReturnType;
                    var domainModel = domainList.GetDomainModelFirst(returnType);
                    if (domainModel == null) return false;
                    if (!Util.Util.IsDomainModelEntityOrValueObjectOrAggregate(domainModel)) return false;
                }
                return true;
            }

            return false;
        }
    }
}
