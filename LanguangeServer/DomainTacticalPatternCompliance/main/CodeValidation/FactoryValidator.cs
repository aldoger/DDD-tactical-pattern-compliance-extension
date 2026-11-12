using DomainTacticalPatternCompliance.main.Identifier;
using DomainTacticalPatternCompliance.main.StructureInfo;
using System;
using DomainTacticalPatternCompliance.parser;
using DomainTacticalPatternCompliance.Util;

namespace DomainTacticalPatternCompliance.main.CodeValidation
{
    public class FactoryValidator : DomainObjectValidate
    {
        public FactoryValidator(Parser par) : base(par)
        { }
        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            Factory fc = (Factory)domainModel;
            bool c16 = ValidateConstraintC16(fc, domainList);
            bool c15 = ValidateConstraintC15(fc, domainList);

            string name = "";
            if (fc.IsMethod) name = fc.MethodInfo.MethodName;
            else if (fc.IsClass) name = fc.ClassInfo.ClassName;

            if (!c16) Util.Util.AddViolation(vlist, name, Constraint.CONSTRAINT_C16);
            if (!c15) Util.Util.AddViolation(vlist, name, Constraint.CONSTRAINT_C15);
        }

        private bool ValidateConstraintC16(Factory fc, DomainModelList domainList)
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
                    if (parser.IsMethodPrivate(method.MethodDeclaration)) continue;
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
