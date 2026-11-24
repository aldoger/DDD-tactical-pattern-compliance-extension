#nullable enable
using DomainTacticalPatternCompliance.main.StructureInfo;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DomainTacticalPatternCompliance.main.Identifier
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

        public static void IdentifyDomainModels(
            ClassStructureInfo classStructureInfo,
            DomainModelList domainModelList,
            ISet<string>? projectTypeNames = null
        )
        {
            var cls = ClassMemberContext.From(classStructureInfo.ClassDeclaration);
            foreach (var m in classStructureInfo.Methods) m.Analyze(cls);
            new ClassStereotypeIdentifier().IdentifyStereotype(classStructureInfo);

            var signals = DomainModelSignalExtractor.Extract(classStructureInfo.ClassDeclaration, projectTypeNames);
            var decision = Decide(signals, classStructureInfo.Stereotype);

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
                        if (!s.AllStateReadOnly) score += 3;
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
                        foreach (var method in info.Methods)
                        {
                            if (method.Facts?.ReturnType != null)
                            {
                                newFactory.AddObjectCreatedType(method.Facts.ReturnType);
                            }
                        }
                        break;
                    }
                case DomainModelKind.Repository:
                    {
                        // Accessed domain models are filled in later by ResolveDomainModelReferences,
                        // because the entities this repository uses may not be registered yet.
                        Repository newRepository = new Repository(info.ClassName, info);
                        list.AddDomainModel(newRepository);
                        break;
                    }
                case DomainModelKind.DomainService:
                    {
                        // Involved domain models are filled in later by ResolveDomainModelReferences.
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

        // ------------------------------------------------------------ reference resolution

        /// <summary>
        /// Second pass, to run after every class has gone through <see cref="IdentifyDomainModels"/>.
        /// It fills in which domain models each Repository accesses and which domain models each
        /// Domain Service works with. It must run afterwards because a class can reference a domain
        /// model that has not been registered yet.
        /// </summary>
        /// <param name="compilation">
        /// Optional. When given, the semantic model also finds types the syntax does not show
        /// (e.g. <c>var r = repo.GetById(id)</c> resolves to <c>Residence</c>).
        /// </param>
        public static void ResolveDomainModelReferences(DomainModelList list, Compilation? compilation = null)
        {
            foreach (var dm in list.DomainModels.ToList())
            {
                switch (dm)
                {
                    case Repository repository:
                        ResolveRepositoryAccess(repository, list);
                        break;
                    case DomainService domainService:
                        ResolveDomainServiceInvolvement(domainService, list, compilation);
                        break;
                }
            }
        }

        /// <summary>
        /// A repository's contract is its method signatures: every parameter and return type is
        /// scanned (e.g. <c>Task&lt;IReadOnlyList&lt;Human&gt;&gt; GetByResidenceAsync(Guid)</c> gives Human).
        /// The main <see cref="Repository.AccessedType"/> is the entity/aggregate that appears in
        /// the most methods; value objects used only as search criteria (e.g. Address) count only
        /// when there is no entity.
        /// </summary>
        private static void ResolveRepositoryAccess(Repository repository, DomainModelList list)
        {
            if (repository.ClassInfo == null) return;

            // how many methods reference each domain model
            var methodHits = new Dictionary<DomainModelStruct, int>();

            foreach (var method in repository.ClassInfo.Methods)
            {
                var decl = method.MethodDeclaration;
                var signatureTypes = decl.ParameterList.Parameters
                    .Select(p => p.Type)
                    .Append(decl.ReturnType)
                    .SelectMany(SyntacticTypeNames)
                    .Distinct();

                foreach (var dm in LookupDomainModels(signatureTypes, list, exclude: repository))
                {
                    if (!Util.Util.IsDomainModelEntityOrValueObjectOrAggregate(dm)) continue;

                    repository.AddAccessedDomainModel(dm);
                    methodHits[dm] = methodHits.TryGetValue(dm, out var c) ? c + 1 : 1;
                }
            }

            var primary = methodHits
                .OrderByDescending(kv => kv.Key is Entity || kv.Key is Aggregate)
                .ThenByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .FirstOrDefault();

            if (primary != null) repository.SetAccessedType(primary.Name);
        }

        /// <summary>
        /// A domain service coordinates several domain models, so the whole class body is scanned:
        /// parameter, return and local variable types, <c>new X()</c>, static calls such as
        /// <c>Address.Create(...)</c> and generic arguments. Every registered domain model that
        /// appears (entity, value object, aggregate, repository, factory, event, other service)
        /// is recorded.
        /// </summary>
        private static void ResolveDomainServiceInvolvement(DomainService domainService, DomainModelList list, Compilation? compilation)
        {
            if (domainService.ClassInfo == null) return;

            var classDecl = domainService.ClassInfo.ClassDeclaration;
            var names = new HashSet<string>(SyntacticTypeNames(classDecl));

            var model = TryGetSemanticModel(classDecl, compilation);
            if (model != null)
                names.UnionWith(SemanticTypeNames(classDecl, model));

            foreach (var dm in LookupDomainModels(names, list, exclude: domainService))
                domainService.AddDomainModelInvolved(dm);
        }

        // ------------------------------------------------------------ helpers

        private static IEnumerable<DomainModelStruct> LookupDomainModels(
            IEnumerable<string> names, DomainModelList list, DomainModelStruct exclude)
        {
            foreach (var name in names)
            {
                var matches = list.GetDomainModel(name);
                if (matches == null) continue;
                foreach (var dm in matches)
                    if (!ReferenceEquals(dm, exclude))
                        yield return dm;
            }
        }

        /// <summary>
        /// Every simple name in the node: type names inside generics, arrays, nullables and
        /// qualified names, plus identifiers used as static receivers (<c>Address</c> in
        /// <c>Address.Create()</c>). Extra names do no harm because they are only kept when
        /// they match a registered domain model.
        /// </summary>
        private static IEnumerable<string> SyntacticTypeNames(SyntaxNode? node)
        {
            if (node == null) return Enumerable.Empty<string>();
            return node.DescendantNodesAndSelf()
                .OfType<SimpleNameSyntax>()
                .Select(n => n.Identifier.ValueText);
        }

        /// <summary>
        /// Types the compiler infers: expressions (catches <c>var</c>), and declared parameters/locals.
        /// </summary>
        private static IEnumerable<string> SemanticTypeNames(SyntaxNode node, SemanticModel model)
        {
            var result = new HashSet<string>();
            foreach (var n in node.DescendantNodes())
            {
                ITypeSymbol? type = n switch
                {
                    ExpressionSyntax e => model.GetTypeInfo(e).Type,
                    VariableDeclaratorSyntax v => (model.GetDeclaredSymbol(v) as ILocalSymbol)?.Type,
                    ParameterSyntax p => model.GetDeclaredSymbol(p)?.Type,
                    _ => null
                };
                CollectTypeNames(type, result);
            }
            return result;
        }

        private static void CollectTypeNames(ITypeSymbol? type, ISet<string> into)
        {
            switch (type)
            {
                case null:
                    return;
                case IArrayTypeSymbol array:
                    CollectTypeNames(array.ElementType, into);
                    return;
                case INamedTypeSymbol named:
                    if (!into.Add(named.Name)) return;
                    foreach (var arg in named.TypeArguments)
                        CollectTypeNames(arg, into);
                    return;
                default:
                    into.Add(type.Name);
                    return;
            }
        }

        private static SemanticModel? TryGetSemanticModel(SyntaxNode node, Compilation? compilation)
        {
            if (compilation == null || !compilation.ContainsSyntaxTree(node.SyntaxTree)) return null;
            return compilation.GetSemanticModel(node.SyntaxTree);
        }
    }
}