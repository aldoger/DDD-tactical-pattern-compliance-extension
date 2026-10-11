using GettingStartedCS.main.Identifier;
using GettingStartedCS.main.StructureInfo;
using GettingStartedCS.parser;


namespace GettingStartedCS.main.CodeValidation
{
    public class EntityValidator : DomainObjectValidate
    {
        public EntityValidator(Parser par) : base(par)
        { }

        public override void Validate(DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            Entity entity = (Entity)domainModel;
            bool c1 = ValidateConstraintC1(entity);
            bool c2 = ValidateConstraintC2(entity);

            if (!c1)
            {
                vlist.AddViolation(
                    new Violation(
                        entity.Name,
                        Constraint.CONSTRAINT_C1,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C1)
                    )
                );
            }

            if (!c2)
            {
                vlist.AddViolation(
                    new Violation(
                        entity.Name,
                        Constraint.CONSTRAINT_C2,
                        ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C2)
                    )
                );
            }
        }

        public bool ValidateConstraintC1(Entity entity)
        {
            if (entity.ClassInfo == null) return false;
            var properties = parser.GetCProperties(entity.ClassInfo.ClassDeclaration);
            foreach(var property in properties)
            {
                if (Util.Util.ContainsId(property.Identifier.Text)) return true;
            }
            return false;
        }

        public bool ValidateConstraintC2(Entity entity)
        {
            var countPropertyId = 0;
            if (entity.ClassInfo == null) return false;
            var properties = parser.GetCProperties(entity.ClassInfo.ClassDeclaration);
            foreach (var property in properties)
            {
                if (Util.Util.ContainsId(property.Identifier.Text)) countPropertyId++;
            }
            return countPropertyId > 0;
        }
    }
}
