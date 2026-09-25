using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public enum Constraint
    {
        // Entity constraints
        CONSTRAINT_C1 = 1,
        CONSTRAINT_C2 = 2,
        // Value Object constraints
        CONSTRAINT_C3 = 3,
        CONSTRAINT_C4 = 4,
    }

    public static class ConstraintExtensions
    {
        public static string GetDescription(this Constraint constraint)
        {
            switch (constraint)
            {
                case Constraint.CONSTRAINT_C1:
                    return "C1. An entity has and only has one identity.";

                case Constraint.CONSTRAINT_C2:
                    return "C2. The identity of an entity should be designed as the composition of one or several of its attributes.";

                case Constraint.CONSTRAINT_C3:
                    return "C3. A value object does not have an identity.";

                case Constraint.CONSTRAINT_C4:
                    return "C4. A value object is immutable.";

                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(constraint),
                        constraint,
                        null
                    );
            }
        }
    }
}
