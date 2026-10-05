using GettingStartedCS.main.IdentifyDomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class DomainEventValidator : DomainObjectValidate
    {
        public override void Validate(StructureInfo.DomainModelStruct domainModel, ViolationList vlist, DomainModelList domainList)
        {
            StructureInfo.DomainEvent domainEvent = (StructureInfo.DomainEvent)domainModel;
            bool c5 = ValidateConstraintC5(domainEvent);
            bool c6 = ValidateConstraintC6(domainEvent);
            bool c7 = ValidateConstraintC7(domainEvent);
            bool c8 = ValidateConstraintC8(domainEvent);
            bool c9 = ValidateConstraintC9(domainEvent);

            if (c5)
            {
                vlist.AddViolation(new Violation(
                    domainEvent.Name, 
                    Constraint.CONSTRAINT_C5, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C5))
                );
            }
            if (c6)
            {
                vlist.AddViolation(new Violation(
                    domainEvent.Name, 
                    Constraint.CONSTRAINT_C6, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C6))
                );
            }
            if (c7)
            {
                vlist.AddViolation(new Violation(
                    domainEvent.Name, 
                    Constraint.CONSTRAINT_C7, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C7))
                );
            }
            if (c8)
            {
                vlist.AddViolation(new Violation(
                    domainEvent.Name, 
                    Constraint.CONSTRAINT_C8, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C8))
                );
            }
            if (c9)
            {
                vlist.AddViolation(new Violation(
                    domainEvent.Name, 
                    Constraint.CONSTRAINT_C9, 
                    ConstraintDescriptions.GetDescription(Constraint.CONSTRAINT_C9))
                );
            }
        }

        public bool ValidateConstraintC5(StructureInfo.DomainEvent domainEvent)
        {
            return domainEvent.HasIdProperty;
        }
        public bool ValidateConstraintC6(StructureInfo.DomainEvent domainEvent)
        {
            bool hasIdProperty = domainEvent.Properties.Any(p => p.PropertyName == "Id" && (p.PropertyType == "Guid" || p.PropertyType == "string"));
            return hasIdProperty;
        }
        public bool ValidateConstraintC7(StructureInfo.DomainEvent domainEvent)
        {
            bool hasTimeDate = domainEvent.Properties.Any(p => p.PropertyType == "DateTime" || p.PropertyType == "DateTimeOffset");
            return hasTimeDate;
        }
        public bool ValidateConstraintC8(StructureInfo.DomainEvent domainEvent)
        {
            bool isImmutable = domainEvent.Properties.All(p => p.IsReadOnly);
            return isImmutable;
        }
        public bool ValidateConstraintC9(StructureInfo.DomainEvent domainEvent)
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
