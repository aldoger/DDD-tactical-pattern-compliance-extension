#nullable enable
using GettingStartedCS.main.StructureInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GettingStartedCS.main.Identifier
{
    public static class DomainModelIdentifier
    {
        /// <summary>Minimum winning score; below this the class is left Unclassified.</summary>
        public const double MinScore = 3.0;

        /// <summary>Minimum gap between best and second-best candidate; otherwise the result is ambiguous.</summary>
        public const double MinMargin = 1.0;

        // A veto removes a candidate completely.
        private const double Veto = double.NegativeInfinity;

        // Penalty when the class name points to a different pattern (e.g. "XxxFactory" scored as Entity).
        private const double NameMismatchPenalty = 3.0;

        public readonly record struct KindScore(DomainModelKind Kind, double Score);

        public readonly record struct Decision(
            DomainModelKind? Kind,
            IReadOnlyList<KindScore> Ranked,
            string Reason);

        /// <param name="projectTypeNames">
        /// Optional: simple names of all project-defined types. Used to count "domain types" in public
        /// method parameters (a Domain Service signal). If null, a naming heuristic is used instead.
        /// Existing callers keep working without it.
        /// </param>
        public static void IdentifyDomainModels(
            ClassStructureInfo classStructureInfo,
            DomainModelList domainModelList,
            ISet<string>? projectTypeNames = null
        )
        {
            var cls = ClassMemberContext.From(classStructureInfo.ClassDeclaration);
            foreach (var m in classStructureInfo.Methods) m.Analyze(cls);
            new ClassStereotypeIdentifier().IdentifyStereotype(classStructureInfo);

            var signals = ClassSignalExtractor.Extract(classStructureInfo.ClassDeclaration, projectTypeNames);
            var decision = Decide(signals, classStructureInfo.Stereotype);

            Debug.WriteLine(
                $"[DomainModelIdentifier] {classStructureInfo.ClassName}: " +
                string.Join(", ", decision.Ranked.Select(r => $"{r.Kind}={Fmt(r.Score)}")) +
                $" => {(decision.Kind?.ToString() ?? "Unclassified")} ({decision.Reason})");

            if (decision.Kind is null) return;

            Register(decision.Kind.Value, classStructureInfo, domainModelList);
        }

        // ------------------------------------------------------------ decision

        public static Decision Decide(DomainModelSignals s, IReadOnlyCollection<ClassStereotype> stereotypes)
        {
            var ranked = Enum.GetValues<DomainModelKind>()
                .Select(k => new KindScore(k, Score(k, s, stereotypes)))
                .OrderByDescending(x => x.Score)
                .ToList();

            if (s.ExplicitKind is { } explicitKind)
                return new Decision(explicitKind, ranked, "explicit attribute");

            var best = ranked[0];
            var second = ranked[1];

            if (double.IsNegativeInfinity(best.Score) || best.Score < MinScore)
                return new Decision(null, ranked, $"best score {Fmt(best.Score)} < {MinScore}");

            if (!double.IsNegativeInfinity(second.Score) && best.Score - second.Score < MinMargin)
                return new Decision(null, ranked, $"ambiguous: {best.Kind} vs {second.Kind}");

            return new Decision(best.Kind, ranked, "scored");
        }

        public static double Score(DomainModelKind kind, DomainModelSignals s, IReadOnlyCollection<ClassStereotype> st)
        {
            double raw = RawScore(kind, s, st);
            if (double.IsNegativeInfinity(raw)) return raw;
            return raw - NamePenalty(kind, s.NameKind);
        }

        private static double RawScore(DomainModelKind kind, DomainModelSignals s, IReadOnlyCollection<ClassStereotype> st)
        {
            bool Has(ClassStereotype x) => st.Contains(x);

            switch (kind)
            {
                // Entity: Entity stereotype and/or identity.
                //   has Id                                   -> entity
                //   no Id, has Equals + GetHashCode          -> looks like a value object (entity score cancelled)
                //   no Id, no Equals/GetHashCode             -> still more likely an entity
                case DomainModelKind.Entity:
                    {
                        bool entityStereotype = Has(ClassStereotype.Entity) || Has(ClassStereotype.MinimalEntity);

                        double score = 0;
                        if (s.HasIdentity) score += 3;
                        if (entityStereotype) score += 3;
                        if (entityStereotype && !s.HasIdentity)
                            score += s.HasStructuralEquality ? -3 : 1;
                        return score;
                    }

                // Value object: never has identity. DataProvider/DataClass stereotype is enough to count
                // as a value object; Equals + GetHashCode (or record/struct/IEquatable) strengthens it.
                case DomainModelKind.ValueObject:
                    {
                        double score = MinScore;
                        if (s.HasIdentity)
                        {
                            score -= 1;
                            if (s.HasStructuralEquality) score += 3;
                            if (s.SelfReturningMethods > 1) score += 3;
                        }
                        return score + (Has(ClassStereotype.DataProvider) || Has(ClassStereotype.DataClass) ? 3 : 0)
                             + (s.HasStructuralEquality ? 3 : 0)
                             + (s.AllStateReadOnly ? 2 : 0)
                             + (s.SelfReturningMethods > 0 ? 1 : 0);
                    }

                // Repository: Get/Find/Update method names are the main behavioural clue.
                case DomainModelKind.Repository:
                    {
                        if (s.HasIdentity) return Veto;
                        return (s.ImplementsRepositoryInterface ? 4 : 0)
                             + (s.NameKind == NameKind.Repository ? 2 : 0)
                             + (s.QueryVerbMethodCount >= 2 && s.QueryVerbRatio >= 0.5 ? 3 : 0)
                             + (s.CrudMethodCount >= 3 ? 1 : 0)
                             + (Has(ClassStereotype.Boundary) ? 1 : 0);
                    }

                // Domain service: Controller/PureController stereotype AND "Service" in the name is the
                // strongest combination; stateless + domain-typed parameters support it.
                case DomainModelKind.DomainService:
                    {
                        if (s.HasIdentity || s.ImplementsRepositoryInterface) return Veto;

                        int stereotypePoints =
                            (Has(ClassStereotype.Controller) || Has(ClassStereotype.PureController)) ? 2 :
                            (Has(ClassStereotype.Commander) || Has(ClassStereotype.Boundary)) ? 1 : 0;

                        double score = (s.IsStateless ? 3 : 0)
                                     + (s.DomainTypesInPublicParams >= 2 ? 2 : 0)
                                     + (s.NameKind == NameKind.Service ? 3 : 0)
                                     + stereotypePoints;

                        // Without domain-typed parameters or a "Service" name, a stateless helper class
                        // must not qualify as a domain service.
                        if (s.DomainTypesInPublicParams == 0 && s.NameKind != NameKind.Service)
                            score = Math.Min(score, MinScore - 1);

                        return score;
                    }

                // Factory: Factory stereotype + every public method creates an object = most likely a factory.
                case DomainModelKind.Factory:
                    {
                        if (s.HasIdentity) return Veto;
                        return (Has(ClassStereotype.Factory) ? 2 : 0)
                             + (s.FactoryMethodRatio >= 0.999 ? 3 : s.FactoryMethodRatio >= 0.66 ? 2 : 0)
                             + (s.NameKind == NameKind.Factory ? 2 : 0)
                             + (s.IsStateless ? 1 : 0);
                    }

                default:
                    return 0;
            }
        }

        private static double NamePenalty(DomainModelKind kind, NameKind name)
        {
            if (name == NameKind.None) return 0;

            DomainModelKind expected = name switch
            {
                NameKind.Service => DomainModelKind.DomainService,
                NameKind.Factory => DomainModelKind.Factory,
                _ => DomainModelKind.Repository
            };

            return kind == expected ? 0 : NameMismatchPenalty;
        }

        // ------------------------------------------------------------ registration

        private static void Register(DomainModelKind kind, ClassStructureInfo info, DomainModelList list)
        {
            switch (kind)
            {
                case DomainModelKind.Factory:
                    {
                        Factory newFactory = new Factory(info.ClassName);
                        newFactory.SetClassInfo(info);
                        list.AddDomainModel(newFactory);
                        break;
                    }
                case DomainModelKind.Repository:
                    {
                        Repository newRepository = new Repository(info.ClassName, info);
                        list.AddDomainModel(newRepository);
                        break;
                    }
                case DomainModelKind.DomainService:
                    {
                        DomainService newDomainService = new DomainService(info.ClassName, info);
                        list.AddDomainModel(newDomainService);
                        break;
                    }
                case DomainModelKind.Entity:
                    {
                        Entity newEntity = new Entity(info.ClassName, info);
                        list.AddDomainModel(newEntity);
                        break;
                    }
                case DomainModelKind.ValueObject:
                    {
                        ValueObject newValueObject = new ValueObject(info.ClassName, info);
                        list.AddDomainModel(newValueObject);
                        break;
                    }
            }
        }

        private static string Fmt(double score) =>
            double.IsNegativeInfinity(score) ? "veto" : score.ToString("0.#");
    }
}