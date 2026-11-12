using DomainTacticalPatternCompliance.main.Identifier;
using System;
using DomainTacticalPatternCompliance.main.StructureInfo;
using DomainTacticalPatternCompliance.parser;

namespace DomainTacticalPatternCompliance.main.CodeValidation
{
    public class ValueObjectValidator : DomainObjectValidate
    {
        public ValueObjectValidator(Parser par) : base(par)
        { }
        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            ValueObject vo = (ValueObject)domainModel;
            bool c1 = ValidateConstraintC3(vo);
            bool c2 = ValidateConstraintC4(vo);

            if (!c1) Util.Util.AddViolation(vlist, vo.Name, Constraint.CONSTRAINT_C3);
            if (!c2) Util.Util.AddViolation(vlist, vo.Name, Constraint.CONSTRAINT_C4);
        }

        private bool ValidateConstraintC3(ValueObject vo)
        {
            if (vo.ClassInfo == null) return false;
            var properties = parser.GetProperties(vo.ClassInfo.ClassDeclaration);
            foreach (var property in properties)
            {
                if(Util.Util.ContainsId(property.Identifier.Text)) return false;
            }
            return true;
        }

        private bool ValidateConstraintC4(ValueObject vo)
        {
            if (vo.ClassInfo == null) return false;
            var properties = parser.GetProperties(vo.ClassInfo.ClassDeclaration);
            foreach (var property in properties)
            {
                if (!parser.IsPropertyImmutable(property)) return false;
            }
            return true;
        }
    }
}
