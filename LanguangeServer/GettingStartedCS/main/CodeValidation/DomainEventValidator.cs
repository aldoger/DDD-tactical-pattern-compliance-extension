using GettingStartedCS.main.Identifier;
using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.parser;
using System;
using System.Xml.Linq;

namespace GettingStartedCS.main.CodeValidation
{
    public class DomainEventValidator : DomainObjectValidate
    {
        public DomainEventValidator(Parser par) : base(par)
        { }
        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            DomainEvent de = (DomainEvent)domainModel;
            bool c5 = ValidateConstraintC5(de);
            bool c6 = ValidateConstraintC6(de);
            bool c7 = ValidateConstraintC7(de);
            bool c8 = ValidateConstraintC8(de);
            bool c9 = ValidateConstraintC9(de);

            if (!c5)
            {
                vlist.AddViolation(
                    new Violation(
                        de.ClassInfo.ClassName,
                        Constraint.CONSTRAINT_C5,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C5)
                    )
                );
            }

            if (!c6)
            {
                vlist.AddViolation(
                    new Violation(
                        de.ClassInfo.ClassName,
                        Constraint.CONSTRAINT_C6,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C6)
                    )
                );
            }

            if (!c7)
            {
                vlist.AddViolation(
                    new Violation(
                        de.ClassInfo.ClassName,
                        Constraint.CONSTRAINT_C7,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C7)
                    )
                );
            }

            if (!c8)
            {
                vlist.AddViolation(
                    new Violation(
                        de.ClassInfo.ClassName,
                        Constraint.CONSTRAINT_C8,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C8)
                    )
                );
            }

            if (!c9)
            {
                vlist.AddViolation(
                    new Violation(
                        de.ClassInfo.ClassName,
                        Constraint.CONSTRAINT_C9,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C9)
                    )
                );
            }
        }

        private bool ValidateConstraintC5(DomainEvent de)
        {
            if (de.ClassInfo == null) return false;
            var properties = parser.GetProperties(de.ClassInfo.ClassDeclaration);
            foreach(var property in properties)
            {
                if (Util.Util.ContainsId(property.Identifier.Text)) return true;
            }
            return false;
        }

        private bool ValidateConstraintC6(DomainEvent de)
        {
            if (de.ClassInfo == null) return false;
            var properties = parser.GetProperties(de.ClassInfo.ClassDeclaration);
            var totalIdProperties = 0;
            foreach (var property in properties)
            {
                if (Util.Util.ContainsId(property.Identifier.Text)) totalIdProperties++;
            }
            return totalIdProperties > 0;
        }

        private bool ValidateConstraintC7(DomainEvent de)
        {
            if (de.ClassInfo == null) return false;
            var properties = parser.GetProperties(de.ClassInfo.ClassDeclaration);
            foreach(var property in properties)
            {
                if (parser.IsTemporalSyntax(property)) return true;
            }
            return false;
        }

        private bool ValidateConstraintC8(DomainEvent de)
        {
            if (de.ClassInfo == null) return false;
            var properties = parser.GetProperties(de.ClassInfo.ClassDeclaration);
            foreach (var property in properties)
            {
                if (!parser.IsPropertyImmutable(property)) return false;
            }
            return true;
        }

        private bool ValidateConstraintC9(DomainEvent de)
        {
            // TODO: on progress for implementation
            return true;
        }
    }
}
