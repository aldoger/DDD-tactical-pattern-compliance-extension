using DomainTacticalPatternCompliance.main.Identifier;
using DomainTacticalPatternCompliance.main.StructureInfo;
using DomainTacticalPatternCompliance.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DomainTacticalPatternCompliance.main.CodeValidation
{
    public class RepositoryValidator : DomainObjectValidate
    {
        public RepositoryValidator(Parser par) : base(par)
        { }
        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            Repository rp = (Repository)domainModel;
            bool c12 = ValidateConstraintC12(rp, domainList);
            bool c13 = ValidateConstraintC13(rp);
            bool c14 = ValidateConstraintC14(rp, domainList);

            if (!c12) Util.Util.AddViolation(vlist, rp.ClassInfo.ClassName, Constraint.CONSTRAINT_C12);
            if (!c13) Util.Util.AddViolation(vlist, rp.ClassInfo.ClassName, Constraint.CONSTRAINT_C13);
            if (!c14) Util.Util.AddViolation(vlist, rp.ClassInfo.ClassName, Constraint.CONSTRAINT_C14);
        }

        private bool ValidateConstraintC12(Repository rp, DomainModelList domainList)
        {
            var found = new HashSet<string>();
            if (rp.ClassInfo == null) return false;

            foreach (var method in rp.ClassInfo.Methods)
            {
                var decl = method.MethodDeclaration;

                var typeNames = parser.GetMethodParameters(decl)
                    .SelectMany(p => parser.GetTypeNames(p.Type))
                    .Concat(parser.GetTypeNames(decl.ReturnType));

                foreach (var name in typeNames)
                {
                    var dm = domainList.GetDomainModelFirst(name);
                    if (dm != null && Util.Util.IsDomainModelEntityOrValueObjectOrAggregate(dm))
                        found.Add(name);
                }
            }
            return found.Count > 0;
        }

        private bool ValidateConstraintC13(Repository rp)
        {
            if (rp.ClassInfo == null) return false;
            var properties = parser.GetProperties(rp.ClassInfo.ClassDeclaration);
            if (properties.Count() > 0) return false;
            return true;
        }

        private bool ValidateConstraintC14(Repository rp, DomainModelList domainList)
        {
            if (rp.ClassInfo == null) return false;
            var domainModels = domainList.GetDomainModel(rp.ClassInfo.ClassName);
            if (domainModels == null || domainModels.Count() > 1) return false;
            return true;
        }
    }
}
