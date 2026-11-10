using DomainTacticalPatternCompliance.main.Identifier;
using System;
using DomainTacticalPatternCompliance.main.StructureInfo;
using DomainTacticalPatternCompliance.parser;
using DomainTacticalPatternCompliance.Main.Identifier;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DomainTacticalPatternCompliance.main.CodeValidation
{
    public class AggregateValidator
    {
        Parser Parser;
        public AggregateValidator(Parser par)
        { 
            Parser = par;
        }

        public void Validate(AggregateIdentificationResult aggResults, ViolationList vlist, DomainModelList domainList)
        {
            
        }

        private bool ValidateConstraintC17(AggregateCandidate aggregate, DomainModelList domainList)
        {
            var domain = domainList.GetDomainModelFirst(aggregate.Root);
            if (domain == null || domain is not Entity) return false;
            return true;
        }

        private bool ValidateConstraintC18(AggregateCandidate aggregate, DomainModelList domainList)
        {
            var parts = aggregate.Parts;
            foreach (var part in parts)
            {
                var domain = domainList.GetDomainModelFirst(part);
                if(domain is not Entity || domain is not ValueObject)
                {
                    return false;
                }
            }
            return true;
        }

        private bool ValidateConstraintC19(AggregateCandidate aggregate, DomainModelList domainList)
        {
            /* 
             The reference of an aggregate part cannot
             be held by the outside objects.
             */
            return true;
        }

        private bool ValidateConstraintC20(AggregateCandidate aggregate)
        {
            var roots = aggregate.MemberTypes.Where(a => a.Value == AggregateType.AGGREGATE_ROOT)
                .Select(a => a.Key).ToList();
            return roots.Count == 0;
        }

        private bool ValidateConstraintC21()
        {
            /* 
             Except the aggregate root, an aggregate can
             only contain aggregate parts.
             */
            return true;
        }

        private bool ValidateConstraintC22(AggregateCandidate aggregate, DomainContext domainContext)
        {
            /*
             The creation of an aggregate should be done
             by a factory.
             */
            return true;
        }

        private bool ValidateConstraintC23(AggregateCandidate aggregate, DomainContext domainContext)
        {
            /*
             The accessing of an aggregate should be
             done by a repository.
             */
            return true;
        }

        private bool ValidateConstraintC24()
        {
            /* 
             The objects within an aggregate should not
             be crosscutting different bounded contexts.
             */
            return true;
        }
    }
}
