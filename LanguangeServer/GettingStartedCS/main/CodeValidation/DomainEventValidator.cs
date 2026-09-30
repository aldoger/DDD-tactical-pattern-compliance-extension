using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class DomainEventValidator : DomainObjectValidate
    {
        public override void Validate(ClassStructureInfo.ClassStructureInfo classInfo, ViolationList vlist)
        {
            bool c5 = ValidateConstraintC5(classInfo);
            bool c6 = ValidateConstraintC6(classInfo);
            bool c7 = ValidateConstraintC7(classInfo);
            bool c8 = ValidateConstraintC8(classInfo);
            bool c9 = ValidateConstraintC9(classInfo);

            if (c5)
            {
                vlist.AddViolation(new Violation(classInfo.ClassName, Constraint.CONSTRAINT_C5, ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C5)));
            }
            if (c6)
            {
                vlist.AddViolation(new Violation(classInfo.ClassName, Constraint.CONSTRAINT_C6, ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C6)));
            }
            if (c7)
            {
                vlist.AddViolation(new Violation(classInfo.ClassName, Constraint.CONSTRAINT_C7, ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C7)));
            }
            if (c8)
            {
                vlist.AddViolation(new Violation(classInfo.ClassName, Constraint.CONSTRAINT_C8, ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C8)));
            }
            if (c9)
            {
                vlist.AddViolation(new Violation(classInfo.ClassName, Constraint.CONSTRAINT_C9, ConstraintExtensions.GetDescription(Constraint.CONSTRAINT_C9)));
            }
        }

        public bool ValidateConstraintC5(ClassStructureInfo.ClassStructureInfo classInfo)
        {
            return classInfo.HasIdProperty;
        }
        public bool ValidateConstraintC6(ClassStructureInfo.ClassStructureInfo classInfo)
        {
            bool hasIdProperty = classInfo.Properties.Any(p => p.PropertyName == "Id" && (p.PropertyType == "Guid" || p.PropertyType == "string"));
            return hasIdProperty;
        }
        public bool ValidateConstraintC7(ClassStructureInfo.ClassStructureInfo classInfo)
        {
            bool hasTimeDate = classInfo.Properties.Any(p => p.PropertyType == "DateTime" || p.PropertyType == "DateTimeOffset");
            return hasTimeDate;
        }
        public bool ValidateConstraintC8(ClassStructureInfo.ClassStructureInfo classInfo)
        {
            bool isImmutable = classInfo.Properties.All(p => p.IsReadOnly);
            return isImmutable;
        }
        public bool ValidateConstraintC9(ClassStructureInfo.ClassStructureInfo classInfo)
        {
            /* 
                TODO: Cari cara identifikasi publisher subscriber
                Masih bingung cara mengecek adanya publisher subscriber di class structure info, karena tidak ada informasi tentang event handler atau delegate di ClassStructureInfo.
                Untuk sekarang return true saja, tapi nanti harus diimplementasikan dengan benar.
             */
            return true;
        }
    }
}
