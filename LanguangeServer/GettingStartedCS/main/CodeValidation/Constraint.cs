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
        // Domain Services
        CONSTRAINT_C10 = 10,
        CONSTRAINT_C11 = 11,
        // Repository constraints
        CONSTRAINT_12 = 12,
        CONSTRAINT_13 = 13,
        CONSTRAINT_14 = 14,
        // Factory constraints
        CONSTRAINT_15 = 15,
        CONSTRAINT_16 = 16,
        // Aggregate constraints
        CONSTRAINT_17 = 17,
        CONSTRAINT_18 = 18,
        CONSTRAINT_19 = 19,
        CONSTRAINT_20 = 20,
        CONSTRAINT_21 = 21,
        CONSTRAINT_22 = 22,
        CONSTRAINT_23 = 23,
        CONSTRAINT_24 = 24,

    }

    public static class ConstraintDescriptions
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
                case Constraint.CONSTRAINT_C10:
                    return "C10. A domain service is stateless";
                case Constraint.CONSTRAINT_C11:
                    return "C11. A domain service should not be designed as another patterns at the same time.";
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
                case Constraint.CONSTRAINT_17:
                    return "17. The root of an aggregate can only be designed as an entity.";
                case Constraint.CONSTRAINT_18:
                    return "18. The aggregate part can only be designed as an entity or a value object.";
                case Constraint.CONSTRAINT_19:
                    return "19. The reference of an aggregate part cannot be held by the outside objects";
                case Constraint.CONSTRAINT_20:
                    return "20. An aggregate has one and only one aggregate root.";
                case Constraint.CONSTRAINT_21:
                    return "21. Except the aggregate root, an aggregate can only contain aggregate parts.";
                case Constraint.CONSTRAINT_22:
                    return "22. The creation of an aggregate should be done by a factory.";
                case Constraint.CONSTRAINT_23:
                    return "23. The accessing of an aggregate should be done by a repository.";
                case Constraint.CONSTRAINT_24:
                    return "24. The objects within an aggregate should not be crosscutting different bounded contexts.";
                default:
                    return "";
            }
        }
    }
}
