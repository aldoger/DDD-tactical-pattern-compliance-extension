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
        // Domain Event constraints
        CONSTRAINT_C5 = 5,
        CONSTRAINT_C6 = 6,
        CONSTRAINT_C7 = 7,
        CONSTRAINT_C8 = 8,
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
                case Constraint.CONSTRAINT_C5:
                    return "C5. A domain event has and only has one identity.";
                case Constraint.CONSTRAINT_C6:
                    return "C6. The identity of a domain event should be designed as the composition of one or several of its attributes.";
                case Constraint.CONSTRAINT_C7:
                    return "C7. A domain event needs a timestamp that records the time when the event happens.";
                case Constraint.CONSTRAINT_C8:
                    return "C8. A domain event is immutable.";

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
