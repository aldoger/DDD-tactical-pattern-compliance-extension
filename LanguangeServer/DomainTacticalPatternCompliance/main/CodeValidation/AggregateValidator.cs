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
        public AggregateValidator()
        { }

        public void Validate(AggregateIdentificationResult aggResults, ViolationList vlist, DomainModelList domainList)
        {
            // Validate constraint C17
            foreach(var aggregate in aggResults.StrictAggregates)
            {
                ValidateConstraintC17(aggregate, vlist, domainList);
            }

            // Validate constraint C18
            foreach (var aggregate in aggResults.StrictAggregates)
            {
                ValidateConstraintC18(aggregate, vlist, domainList);
            }

            // Validate constraint C19
            foreach (var aggregate in aggResults.StrictAggregates)
            {
                
            }

            // Validate constraint C20
            foreach (var aggregate in aggResults.StrictAggregates)
            {
                if(!ValidateConstraintC20(aggregate))
                {
                    // need modify name of aggregate to be more descriptive, like AggregateRootName
                    Util.Util.AddViolation(vlist, aggregate.Root, Constraint.CONSTRAINT_C20);
                }
            }

            // Validate constraint C21
            foreach (var aggregate in aggResults.StrictAggregates)
            {

            }

            // Validate constraint C22
            ValidateConstraintC22(aggResults, vlist);

            // Validate constraint C23
            ValidateConstraintC23(aggResults, vlist);

            // Validate constraint C24
        }

        private void ValidateConstraintC17(AggregateCandidate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            var domain = domainList.GetDomainModelFirst(aggregate.Root);
            if (domain == null || domain is not Entity)
            {
                Util.Util.AddViolation(vlist, aggregate.Root, Constraint.CONSTRAINT_C17);
            }
        }

        private void ValidateConstraintC18(AggregateCandidate aggregate, ViolationList vlist, DomainModelList domainList)
        {
            var parts = aggregate.Parts;
            foreach (var part in parts)
            {
                var domain = domainList.GetDomainModelFirst(part);
                if (domain is not Entity && domain is not ValueObject)
                {
                    Util.Util.AddViolation(vlist, part, Constraint.CONSTRAINT_C18);
                }
            }
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
            return roots.Count == 1;
        }

        private bool ValidateConstraintC21()
        {
            /* 
             Except the aggregate root, an aggregate can
             only contain aggregate parts.
             */
            return true;
        }

        private void ValidateConstraintC22(AggregateIdentificationResult aggResult, ViolationList vlist)
        {
            var aggregates = aggResult.StrictAggregates;
            foreach(var aggregate in aggregates)
            {
                var root = aggregate.Root;
                if (aggResult.FactoriesCreating(root).Count == 0)
                {
                    Util.Util.AddViolation(vlist, root, Constraint.CONSTRAINT_C22);
                }

                var parts = aggregate.Parts;
                foreach(var part in parts)
                {
                    if(aggResult.FactoriesCreating(part).Count == 0)
                    {
                        Util.Util.AddViolation(vlist, part, Constraint.CONSTRAINT_C22);
                    }
                }
            }
        }

        private void ValidateConstraintC23(AggregateIdentificationResult aggResult, ViolationList vlist)
        {
            foreach (var aggregate in aggResult.StrictAggregates)
            {
                var root = aggregate.Root;
                if (aggResult.RepositoriesAccessing(root).Count == 0)
                    Util.Util.AddViolation(vlist, root, Constraint.CONSTRAINT_C23);
            }
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
