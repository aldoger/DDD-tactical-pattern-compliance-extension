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
        CONSTRAINT_C9 = 9,
        // Repository constraints
        CONSTRAINT_12 = 12,
        CONSTRAINT_13 = 13,
        CONSTRAINT_14 = 14,
        // Factory constraints
        CONSTRAINT_15 = 15,
        CONSTRAINT_16 = 16,
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
                case Constraint.CONSTRAINT_C9:
                    return "C9. A domain event needs to specify the publisher and subscriber of the event.";
                case Constraint.CONSTRAINT_12:
                    return "12. A repository needs to specify the object that it accesses. The object can be entity, value object, and aggregate root";
                case Constraint.CONSTRAINT_13:
                    return "13. A repository has no attributes";
                case Constraint.CONSTRAINT_14:
                    return "14. A repository should not be designed as another patterns at the same time.";
                case Constraint.CONSTRAINT_15:
                    return "15. A factory needs to specify the object that it creates. The type of the object can be entity,value object, and aggregate root.";
                case Constraint.CONSTRAINT_16:
                    return "16. A factory should not be designed as other patterns at the same time.";
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
