using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DomainTacticalPatternCompliance.main.Identifier;
using DomainTacticalPatternCompliance.main.StructureInfo;
using DomainTacticalPatternCompliance.Main.StructureInfo;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainTacticalPatternCompliance.Main.Identifier
{
    // =====================================================================================
    //  Identifikasi aggregate berdasarkan Iliopoulos (2025), Bab 4, varian DOMAIN-ONLY:
    //
    //   Fase 1  Call graph per "unit perilaku domain" (pengganti endpoint)
    //   Fase 2  ClusteringGraph awal       : REFERENCE / VALUE / EMBEDDED
    //   Fase 3  Graph enhancement          : OWNERSHIP / COUPLED + bobot REFERENCE
    //   Fase 4  Clustering                 : Strict Aggregate Clustering (SAC)
    //
    //  Bagian yang BUKAN dari paper ditandai [EKSTENSI].
    //
    //  [EKSTENSI] Fase 1–2 juga mengisi informasi akses di DomainModelList:
    //    DomainService.DomainModelInvolved, Repository.AccessedDomainModels, Repository.AccessedType
    //    (lihat DomainModelAccessResolver). Menggantikan DomainModelIdentifier.ResolveDomainModelReferences.
    // =====================================================================================

    public sealed class AggregateIdentifierOptions
    {
        /// <summary>
        /// [EKSTENSI] Factory class yang membuat beberapa entity dalam satu method dianggap bukti OWNERSHIP:
        /// entity yang dikembalikan → entity lain yang ikut dibuat.
        /// </summary>
        public bool FactoryCreationAsOwnership { get; init; } = true;

        /// <summary>
        /// REFERENCE juga dari parameter method entity ("field or method argument", paper 4.2.2).
        /// </summary>
        public bool MethodParameterAsReference { get; init; } = true;

        /// <summary>
        /// [EKSTENSI] false (bawaan): node value object & enum dibuat PER PEMILIK, mis. "Money@Product" dan
        /// "Money@Payment". Instance VO selalu milik satu entity, jadi tipe VO yang dipakai banyak entity
        /// tidak boleh menyatukan entity-entity itu lewat edge kuat (EMBEDDED/VALUE) di SAC.
        /// true: satu node per tipe, persis seperti graf di paper.
        /// </summary>
        public bool ShareValueObjectNodes { get; init; } = false;

        /// <summary>
        /// [EKSTENSI] Cakupan pengisian <c>DomainService.DomainModelInvolved</c> dan
        /// <c>Repository.AccessedDomainModels</c>.
        /// false (bawaan): LANGSUNG — hanya yang disebut di method class itu sendiri (termasuk helper privat
        /// di class yang sama). Cocok untuk cek kepatuhan, mis. "domain service hanya memakai aggregate root".
        /// true: TRANSITIF — ikut isi method lain yang dipanggil (entity, service lain), sama seperti
        /// "transaksi" di Fase 3.
        /// </summary>
        public bool TransitiveDomainModelAccess { get; init; } = false;
    }

    /// <summary>
    /// [EKSTENSI] Domain model yang diakses sebuah repository / domain service.
    /// Daftar diisi di Fase 1 (call graph); <c>PrimaryType</c> repository ditentukan di Fase 2 (signature).
    /// </summary>
    public sealed record DomainModelAccessInfo(
        string Owner,
        string Kind,
        string? PrimaryType,
        IReadOnlyList<string> Entities,
        IReadOnlyList<string> ValueObjects,
        IReadOnlyList<string> DomainClasses);

    public readonly record struct CouplingScore(
        string A, string B, int Together, int TotalA, int TotalB, double Score, bool Promoted);

    public readonly record struct IdReference(string FromEntity, string ToEntity, string Member);

    /// <summary>
    /// [EKSTENSI] Referensi BERARAH yang disimpan entity: field/properti <c>Member</c> milik <c>FromEntity</c>
    /// menyimpan objek <c>ToEntity</c> (ViaId = false) atau ID-nya (ViaId = true). Dipakai untuk C19.
    /// </summary>
    public readonly record struct EntityReference(string FromEntity, string ToEntity, string Member, bool ViaId);

    public sealed class AggregateCandidate
    {
        public string? Root { get; init; }
        public string RootReason { get; init; } = "";
        public List<string> Entities { get; init; } = new();
        public List<string> ValueObjects { get; init; } = new();
        public List<string> Enums { get; init; } = new();
        public List<string> Warnings { get; init; } = new();
        public IEnumerable<string> Members => Entities.Concat(ValueObjects).Concat(Enums);
        public Dictionary<string, AggregateType> MemberTypes { get; init; } = new();
        public AggregateType? TypeOf(string member) => MemberTypes.TryGetValue(member, out var t) ? t : null;
        public IEnumerable<string> Parts => MemberTypes.Where(kv => kv.Value == AggregateType.AGGREGATE_PART).Select(kv => kv.Key);
    }

    public sealed class AggregateIdentificationResult
    {
        public List<CallGraphNode> CallGraphs { get; init; } = new();
        public List<CallGraphNode> ConstructorGraphs { get; init; } = new();
        public ClusteringGraph InitialGraph { get; init; } = new();
        public ClusteringGraph EnhancedGraph { get; init; } = new();
        public List<string> OwnershipLog { get; init; } = new();
        public List<CouplingScore> CouplingScores { get; init; } = new();
        public double DynamicThreshold { get; init; }
        public List<IdReference> IdReferences { get; init; } = new();
        public List<AggregateCandidate> StrictAggregates { get; init; } = new();

        /// <summary>[EKSTENSI] Fase 2: semua referensi berarah entity → entity (objek atau ID). Dipakai C19.</summary>
        public List<EntityReference> EntityReferences { get; init; } = new();

        /// <summary>[EKSTENSI] Fase 1–2: domain model yang diakses tiap repository / domain service.</summary>
        public List<DomainModelAccessInfo> DomainModelAccess { get; init; } = new();
        public bool DomainModelAccessTransitive { get; init; }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> RepositoryAccess { get; init; } = NoEntries;

        public IReadOnlyDictionary<string, IReadOnlyList<string>> FactoryCreation { get; init; } = NoEntries;

        public IReadOnlyList<string> RepositoriesAccessing(string type) => Lookup(RepositoryAccess, type);
        public IReadOnlyList<string> FactoriesCreating(string type) => Lookup(FactoryCreation, type);

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> NoEntries =
            new Dictionary<string, IReadOnlyList<string>>();

        private static IReadOnlyList<string> Lookup(IReadOnlyDictionary<string, IReadOnlyList<string>> map, string type) =>
            map.TryGetValue(DomainContext.TypeOf(type), out var l) ? l : Array.Empty<string>();

        public string ToReport(bool showCallGraphs = true)
        {
            var sb = new StringBuilder();
            void Header(string t) { sb.AppendLine(); sb.AppendLine(new string('═', 72)); sb.AppendLine(t); sb.AppendLine(new string('═', 72)); }

            Header("FASE 1 — Call graph per unit perilaku domain");
            sb.AppendLine($"{CallGraphs.Count} unit perilaku + {ConstructorGraphs.Count} constructor entity (constructor hanya untuk CreationRecord).");
            if (showCallGraphs)
            {
                int i = 1;
                foreach (var cg in CallGraphs)
                {
                    sb.AppendLine($"\n#{i++} ({cg.UnitKind})");
                    cg.Print(sb);
                    sb.AppendLine($"    ⇒ entity dalam unit ini: [{string.Join(", ", cg.AllEntities().OrderBy(x => x))}]");
                }
            }

            if (DomainModelAccess.Count > 0)
            {
                static string Join(IReadOnlyList<string> xs) => xs.Count > 0 ? string.Join(", ", xs) : "-";
                sb.AppendLine($"\n[EKSTENSI] Akses domain model — cakupan " +
                              (DomainModelAccessTransitive ? "TRANSITIF (ikut method yang dipanggil):" : "LANGSUNG (method class itu sendiri):"));
                foreach (var a in DomainModelAccess)
                {
                    sb.AppendLine($"  {a.Owner} ({a.Kind})" + (a.PrimaryType != null ? $"   tipe utama: {a.PrimaryType}" : ""));
                    sb.AppendLine($"     entity      : {Join(a.Entities)}");
                    if (a.ValueObjects.Count > 0) sb.AppendLine($"     value obj   : {Join(a.ValueObjects)}");
                    if (a.DomainClasses.Count > 0) sb.AppendLine($"     domain class: {Join(a.DomainClasses)}");
                }
            }

            Header("FASE 2 — ClusteringGraph awal");
            sb.AppendLine($"Node: [{string.Join(", ", InitialGraph.Nodes)}]");
            foreach (var e in InitialGraph.Edges.OrderBy(e => e.Type).ThenBy(e => e.A).ThenBy(e => e.B))
                sb.AppendLine($"  {e.Type.ToText(),-10} {e.A} — {e.B}   ({e.Reason})");
            if (EntityReferences.Count > 0)
            {
                sb.AppendLine("Referensi berarah yang disimpan entity (dipakai C19; referensi ID BUKAN edge — DDD menganjurkan antar-aggregate lewat ID):");
                foreach (var r in EntityReferences) sb.AppendLine($"  {r.FromEntity}.{r.Member} → {r.ToEntity}   [{(r.ViaId ? "ID" : "objek")}]");
            }

            Header("FASE 3 — Graph enhancement");
            sb.AppendLine("Langkah 1 — OWNERSHIP:");
            if (OwnershipLog.Count == 0) sb.AppendLine("  (tidak ada)");
            foreach (var l in OwnershipLog) sb.AppendLine("  " + l);
            sb.AppendLine("\nLangkah 2 — coupling score untuk REFERENCE yang tersisa:");
            if (CouplingScores.Count == 0) sb.AppendLine("  (tidak ada edge REFERENCE)");
            else
            {
                sb.AppendLine($"  {"pasangan",-28}{"bersama",8}{"tot A",7}{"tot B",7}{"skor",8}   hasil");
                foreach (var s in CouplingScores)
                    sb.AppendLine($"  {s.A + " — " + s.B,-28}{s.Together,8}{s.TotalA,7}{s.TotalB,7}{s.Score,8:0.000}   " +
                                  (s.Promoted ? "⇒ COUPLED (1.5)" : $"tetap REFERENCE, bobot {s.Score:0.000}"));
                sb.AppendLine($"  Dynamic threshold = rata-rata {CouplingScores.Count} skor = {DynamicThreshold:0.000}");
            }
            sb.AppendLine("\nGraf akhir:");
            foreach (var e in EnhancedGraph.Edges.OrderBy(e => e.Type).ThenBy(e => e.A).ThenBy(e => e.B))
                sb.AppendLine($"  {e.Type.ToText(),-10} {e.A} — {e.B}   w={e.Weight:0.###}");

            Header("FASE 4 — Strict Aggregate Clustering (SAC)");
            PrintCandidates(sb, StrictAggregates);
            return sb.ToString();
        }

        private static void PrintCandidates(StringBuilder sb, List<AggregateCandidate> list)
        {
            int i = 1;
            foreach (var a in list)
            {
                string Tagged(IEnumerable<string> xs) => string.Join(", ", xs.Select(x => a.TypeOf(x) is { } t ? $"{x} [{t.ToText()}]" : x));
                sb.AppendLine($"  Aggregate {i++}: root = {a.Root ?? "-"} ({a.RootReason})");
                sb.AppendLine($"     entity   : {(a.Entities.Count > 0 ? Tagged(a.Entities) : "-")}");
                if (a.ValueObjects.Count > 0) sb.AppendLine($"     value obj: {Tagged(a.ValueObjects)}");
                if (a.Enums.Count > 0) sb.AppendLine($"     enum     : {Tagged(a.Enums)}");
                foreach (var w in a.Warnings) sb.AppendLine($"     ⚠ {w}");
            }
        }
    }

    // =====================================================================================
    //  Pintu masuk
    // =====================================================================================

    public class AggregateIdentifier
    {
        private readonly Compilation _compilation;
        private readonly AggregateIdentifierOptions _options;
        public AggregateIdentifier(Compilation compilation, AggregateIdentifierOptions? options = null)
        {
            _compilation = compilation;
            _options = options ?? new AggregateIdentifierOptions();
        }

        public AggregateIdentificationResult Identify(DomainModelList dms)
        {
            var ctx = new DomainContext(dms, _compilation);

            // Fase 1
            var builder = new CallGraphBuilder(ctx, _options);
            var (units, ctors) = builder.BuildAll();
            // [EKSTENSI] isi DomainService.DomainModelInvolved & Repository.AccessedDomainModels dari call graph
            var access = DomainModelAccessResolver.FillAccessedModels(dms, ctx, builder, _options);

            // Fase 2
            var graph = ClusteringGraphInitializer.Build(ctx, _options, out var idRefs, out var entityRefs);
            var initial = graph.Clone();
            // [EKSTENSI] isi Repository.AccessedType dari signature method repository (analisis struktural)
            DomainModelAccessResolver.FillRepositoryType(dms, ctx, access);

            // Fase 3
            var enh = GraphEnhancer.Enhance(graph, units, ctors);

            // Fase 4
            var sac = StrictAggregateClustering.Cluster(graph);

            return new AggregateIdentificationResult
            {
                RepositoryAccess = ctx.RepositoryAccessSnapshot(),
                FactoryCreation = ctx.FactoryCreationSnapshot(),
                CallGraphs = units,
                ConstructorGraphs = ctors,
                InitialGraph = initial,
                EnhancedGraph = graph,
                OwnershipLog = enh.OwnershipLog,
                CouplingScores = enh.Scores,
                DynamicThreshold = enh.Threshold,
                IdReferences = idRefs,
                EntityReferences = entityRefs,
                DomainModelAccess = access,
                DomainModelAccessTransitive = _options.TransitiveDomainModelAccess,
                StrictAggregates = sac.Select(c => Describe(c, graph, ctx)).OrderByDescending(a => a.Members.Count()).ThenBy(a => a.Root).ToList()
            };
        }

        // ---------------------------------------------------------------- deskripsi cluster

        private static AggregateCandidate Describe(HashSet<string> cluster, ClusteringGraph g, DomainContext ctx)
        {
            var entities = cluster.Where(ctx.IsEntity).OrderBy(x => x, StringComparer.Ordinal).ToList();
            var vos = cluster.Where(ctx.IsValueObject).Select(DomainContext.TypeOf).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            var enums = cluster.Where(x => !ctx.IsEntity(x) && !ctx.IsValueObject(x)).Select(DomainContext.TypeOf).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();

            string? root = null;
            string reason = "tanpa entity";
            var warnings = new List<string>();

            if (entities.Count > 0)
            {
                var ownInCluster = g.Edges.Where(e => e.Type == EdgeType.Ownership && cluster.Contains(e.A) && cluster.Contains(e.B)).ToList();
                int Creates(string e) => ownInCluster.Count(x => x.Owner == e);
                bool IsCreated(string e) => ownInCluster.Any(x => x.Other(x.Owner!) == e);
                double Degree(string e) => g.EdgesOf(e).Where(x => cluster.Contains(x.Other(e))).Sum(x => x.Weight);

                // root: tidak dibuat oleh entity lain di cluster → paling banyak membuat → derajat terbesar → nama
                root = entities
                    .OrderBy(e => IsCreated(e) ? 1 : 0)
                    .ThenByDescending(Creates)
                    .ThenByDescending(Degree)
                    .ThenBy(e => e, StringComparer.Ordinal)
                    .First();

                reason = entities.Count == 1 ? "satu-satunya entity"
                       : Creates(root) > 0 ? $"mengendalikan siklus hidup {string.Join(", ", ownInCluster.Where(x => x.Owner == root).Select(x => x.Other(root)))}"
                       : "derajat terbesar di cluster";
            }
            else warnings.Add("cluster tanpa entity (hanya value object/enum)");

            var memberTypes = new Dictionary<string, AggregateType>();
            if (root != null)
                foreach (var m in entities.Concat(vos).Concat(enums))
                    memberTypes[m] = m == root ? AggregateType.AGGREGATE_ROOT : AggregateType.AGGREGATE_PART;

            return new AggregateCandidate
            {
                Root = root,
                RootReason = reason,
                Entities = entities,
                ValueObjects = vos,
                Enums = enums,
                Warnings = warnings,
                MemberTypes = memberTypes
            };
        }
    }

    // =====================================================================================
    //  Konteks domain: siapa entity, VO, enum, repository; SemanticModel per syntax tree
    // =====================================================================================

    internal sealed class DomainContext
    {
        private readonly Compilation _compilation;
        private readonly Dictionary<SyntaxTree, SemanticModel> _models = new();
        private readonly Dictionary<string, ClassStructureInfo> _classes = new(StringComparer.Ordinal);
        private readonly Dictionary<SyntaxNode, MethodStructureInfo> _methodInfos = new();
        private readonly Dictionary<string, List<string>> _repoAccess = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<string>> _factoryCreated = new(StringComparer.Ordinal);
        /// <summary>[EKSTENSI] repository → (entity/VO → jumlah method yang menyebutnya di signature).</summary>
        private readonly Dictionary<string, Dictionary<string, int>> _repoSignatureHits = new(StringComparer.Ordinal);
        /// <summary>[EKSTENSI] interface (simbol, termasuk argumen generic) → class domain yang mengimplementasikannya.</summary>
        private readonly Dictionary<ISymbol, List<string>> _implementations = new(SymbolEqualityComparer.Default);
        private static readonly IReadOnlyDictionary<string, int> NoHits = new Dictionary<string, int>();
        public IReadOnlyDictionary<string, IReadOnlyList<string>> RepositoryAccessSnapshot() => Freeze(_repoAccess);
        public IReadOnlyDictionary<string, IReadOnlyList<string>> FactoryCreationSnapshot() => Freeze(_factoryCreated);
        public HashSet<string> Entities { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ValueObjects { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Enums { get; } = new(StringComparer.Ordinal);
        public HashSet<string> DomainServices { get; } = new(StringComparer.Ordinal);
        public HashSet<string> FactoryClasses { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Repositories { get; } = new(StringComparer.Ordinal);

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> Freeze(Dictionary<string, List<string>> map) =>
          map.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value.ToArray(), StringComparer.Ordinal);

        private static void AddUnique(Dictionary<string, List<string>> map, string key, string value)
        {
            if (!map.TryGetValue(key, out var list)) map[key] = list = new List<string>();
            if (!list.Contains(value)) list.Add(value);
        }

        private IEnumerable<string> InstantiatedTypes(BaseMethodDeclarationSyntax decl)
        {
            SyntaxNode? body = (SyntaxNode?)decl.Body ?? decl.ExpressionBody;
            if (body == null) yield break;

            var sm = Model(decl.SyntaxTree);
            foreach (var creation in body.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
            {
                string? name = sm.GetTypeInfo(creation).Type?.Name;
                if (string.IsNullOrEmpty(name) && creation is ObjectCreationExpressionSyntax oc)
                    name = TypeNames(oc.Type).FirstOrDefault();
                if (!string.IsNullOrEmpty(name)) yield return name;
            }
        }


        public DomainContext(DomainModelList dms, Compilation compilation)
        {
            _compilation = compilation;

            foreach (var dm in dms.DomainModels)
            {
                ClassStructureInfo? info = dm switch
                {
                    Entity e => e.ClassInfo,
                    ValueObject v => v.ClassInfo,
                    DomainService s => s.ClassInfo,
                    Factory f when f.IsClass => f.ClassInfo,
                    Repository r => r.ClassInfo,
                    _ => null
                };
                if (info == null) continue;

                _classes.TryAdd(dm.Name, info);
                foreach (var m in info.Methods) _methodInfos[m.MethodDeclaration] = m;

                switch (dm)
                {
                    case Entity: Entities.Add(dm.Name); break;
                    case ValueObject: ValueObjects.Add(dm.Name); break;
                    case DomainService: DomainServices.Add(dm.Name); break;
                    case Factory: FactoryClasses.Add(dm.Name); break;
                    case Repository: Repositories.Add(dm.Name); break;
                }
            }

            foreach (var tree in compilation.SyntaxTrees)
                foreach (var en in tree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>())
                    Enums.Add(en.Identifier.Text);

            foreach (var repo in Repositories)
            {
                var hits = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var m in _classes[repo].Methods)
                {
                    var names = m.MethodDeclaration.ParameterList.Parameters.SelectMany(p => TypeNames(p.Type))
                        .Concat(TypeNames(m.MethodDeclaration.ReturnType))
                        .Distinct(StringComparer.Ordinal)
                        .ToList();
                    foreach (var n in names.Where(Entities.Contains))
                        AddUnique(_repoAccess, n, repo);
                    // [EKSTENSI] dipakai Fase 2 untuk menentukan Repository.AccessedType
                    foreach (var n in names.Where(x => Entities.Contains(x) || ValueObjects.Contains(x)))
                        hits[n] = hits.GetValueOrDefault(n) + 1;
                }
                _repoSignatureHits[repo] = hits;
            }

            foreach (var factory in FactoryClasses)
                foreach (var m in _classes[factory].Methods)
                {
                    var created = TypeNames(m.MethodDeclaration.ReturnType)
                        .Concat(InstantiatedTypes(m.MethodDeclaration))
                        .Where(n => Entities.Contains(n) || ValueObjects.Contains(n));
                    foreach (var n in created)
                        AddUnique(_factoryCreated, n, factory);
                }

            // [EKSTENSI] interface → implementasi, supaya pemanggilan lewat interface (dependency injection),
            // mis. IOrderRepository.GetById(...), tetap tercatat sebagai pemakaian OrderRepository.
            foreach (var name in DomainServices.Concat(Repositories).Concat(FactoryClasses))
            {
                var decl = _classes[name].ClassDeclaration;
                if (Model(decl.SyntaxTree).GetDeclaredSymbol(decl) is not { } sym) continue;
                foreach (var iface in sym.AllInterfaces)
                {
                    if (!_implementations.TryGetValue(iface, out var impls)) _implementations[iface] = impls = new List<string>();
                    if (!impls.Contains(name)) impls.Add(name);
                }
            }
        }

        public SemanticModel Model(SyntaxTree tree)
        {
            if (!_models.TryGetValue(tree, out var m))
                _models[tree] = m = _compilation.GetSemanticModel(tree);
            return m;
        }

        /// <summary>Nama node VO/enum milik entity tertentu: "Money@Product".</summary>
        public static string OwnedNode(string type, string owner) => $"{type}@{owner}";
        /// <summary>"Money@Product" → "Money"; nama lain dikembalikan apa adanya.</summary>
        public static string TypeOf(string node) { int i = node.IndexOf('@'); return i < 0 ? node : node[..i]; }

        public bool IsEntity(string n) => Entities.Contains(n);
        public bool IsValueObject(string n) => ValueObjects.Contains(TypeOf(n));
        public bool IsDomainClass(string n) => _classes.ContainsKey(n);
        public ClassStructureInfo? Class(string n) => _classes.TryGetValue(n, out var c) ? c : null;
        public MethodStructureInfo? MethodInfo(SyntaxNode decl) => _methodInfos.TryGetValue(decl, out var m) ? m : null;
        public IReadOnlyList<string> RepositoriesAccessing(string entity) =>
            _repoAccess.TryGetValue(entity, out var l) ? l : Array.Empty<string>();

        /// <summary>[EKSTENSI] Entity/VO yang disebut di signature method repository, dengan jumlah method-nya.</summary>
        public IReadOnlyDictionary<string, int> RepositorySignatureHits(string repo) =>
            _repoSignatureHits.TryGetValue(repo, out var h) ? h : NoHits;

        /// <summary>[EKSTENSI] Domain service / repository / factory class yang mengimplementasikan interface ini.</summary>
        public IReadOnlyList<string> ImplementationsOf(ISymbol iface) =>
            _implementations.TryGetValue(iface, out var l) ? l : Array.Empty<string>();

        /// <summary>Nama entity yang disebut dalam sebuah tipe simbol, termasuk argumen generic dan array.</summary>
        public IEnumerable<string> EntityNamesIn(ITypeSymbol? t) => NamesIn(t, Entities);
        public IEnumerable<string> EntityNamesIn(TypeSyntax? t) => TypeNames(t).Where(Entities.Contains);

        /// <summary>[EKSTENSI] Sama seperti <see cref="EntityNamesIn(ITypeSymbol?)"/>, untuk value object.</summary>
        public IEnumerable<string> ValueObjectNamesIn(ITypeSymbol? t) => NamesIn(t, ValueObjects);
        public IEnumerable<string> ValueObjectNamesIn(TypeSyntax? t) => TypeNames(t).Where(ValueObjects.Contains);

        private static IEnumerable<string> NamesIn(ITypeSymbol? t, HashSet<string> names)
        {
            switch (t)
            {
                case null: yield break;
                case IArrayTypeSymbol arr:
                    foreach (var n in NamesIn(arr.ElementType, names)) yield return n;
                    break;
                case INamedTypeSymbol nt:
                    if (names.Contains(nt.Name)) yield return nt.Name;
                    foreach (var arg in nt.TypeArguments)
                        foreach (var n in NamesIn(arg, names)) yield return n;
                    break;
            }
        }

        /// <summary>Semua nama tipe dalam sebuah TypeSyntax (Task&lt;List&lt;Order?&gt;&gt; → Task, List, Order).</summary>
        public static IEnumerable<string> TypeNames(TypeSyntax? type)
        {
            switch (type)
            {
                case null: yield break;
                case IdentifierNameSyntax id: yield return id.Identifier.Text; break;
                case GenericNameSyntax g:
                    yield return g.Identifier.Text;
                    foreach (var a in g.TypeArgumentList.Arguments)
                        foreach (var n in TypeNames(a)) yield return n;
                    break;
                case QualifiedNameSyntax q: foreach (var n in TypeNames(q.Right)) yield return n; break;
                case AliasQualifiedNameSyntax aq: foreach (var n in TypeNames(aq.Name)) yield return n; break;
                case NullableTypeSyntax nt: foreach (var n in TypeNames(nt.ElementType)) yield return n; break;
                case ArrayTypeSyntax at: foreach (var n in TypeNames(at.ElementType)) yield return n; break;
                case RefTypeSyntax rt: foreach (var n in TypeNames(rt.Type)) yield return n; break;
                case TupleTypeSyntax tt:
                    foreach (var el in tt.Elements)
                        foreach (var n in TypeNames(el.Type)) yield return n;
                    break;
                case PredefinedTypeSyntax pt: yield return pt.Keyword.Text; break;
            }
        }

        public static bool IsPublic(MemberDeclarationSyntax m) => m.Modifiers.Any(SyntaxKind.PublicKeyword);
    }

    // =====================================================================================
    //  FASE 1 — Call graph
    // =====================================================================================

    /// <summary>
    /// [EKSTENSI] Domain model yang dipakai LANGSUNG oleh satu method: signature + isi method itu sendiri,
    /// tanpa isi method lain yang dipanggilnya. Tidak bergantung jalur panggilan, jadi disimpan per deklarasi.
    /// </summary>
    internal sealed class MethodUsage
    {
        public HashSet<string> Entities { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ValueObjects { get; } = new(StringComparer.Ordinal);
        /// <summary>Domain service / repository / factory class LAIN yang dipanggil.</summary>
        public HashSet<string> DomainClasses { get; } = new(StringComparer.Ordinal);

        public void UnionWith(MethodUsage other)
        {
            Entities.UnionWith(other.Entities);
            ValueObjects.UnionWith(other.ValueObjects);
            DomainClasses.UnionWith(other.DomainClasses);
        }
    }

    internal sealed class CallGraphBuilder
    {
        private readonly DomainContext _ctx;
        private readonly AggregateIdentifierOptions _opt;
        private readonly Dictionary<SyntaxNode, MethodUsage> _ownUsage = new();
        private readonly Dictionary<string, List<CallGraphNode>> _unitsByClass = new(StringComparer.Ordinal);

        public CallGraphBuilder(DomainContext ctx, AggregateIdentifierOptions opt)
        {
            _ctx = ctx; _opt = opt;
        }

        /// <summary>[EKSTENSI] Pemakaian langsung sebuah method (null bila method itu tidak pernah ditelusuri).</summary>
        public MethodUsage? OwnUsageOf(SyntaxNode decl) => _ownUsage.TryGetValue(decl, out var u) ? u : null;

        /// <summary>[EKSTENSI] Unit (root call graph) milik sebuah class.</summary>
        public IReadOnlyList<CallGraphNode> UnitsOf(string className) =>
            _unitsByClass.TryGetValue(className, out var l) ? l : Array.Empty<CallGraphNode>();

        private MethodUsage OwnUsage(SyntaxNode decl)
        {
            if (!_ownUsage.TryGetValue(decl, out var u)) _ownUsage[decl] = u = new MethodUsage();
            return u;
        }

        private void Use(MethodUsage own, ITypeSymbol? t)
        {
            own.Entities.UnionWith(_ctx.EntityNamesIn(t));
            own.ValueObjects.UnionWith(_ctx.ValueObjectNamesIn(t));
        }

        private void Use(MethodUsage own, TypeSyntax? t)
        {
            own.Entities.UnionWith(_ctx.EntityNamesIn(t));
            own.ValueObjects.UnionWith(_ctx.ValueObjectNamesIn(t));
        }

        /// <summary>
        /// Unit perilaku domain (root call graph):
        ///   - method publik entity, kecuali yang hanya "get" (mengembalikan field saja)
        ///   - method publik domain service dan factory class
        ///   - method publik repository
        /// Constructor entity ditelusuri terpisah: hanya untuk CreationRecord, bukan transaksi.
        /// </summary>
        public (List<CallGraphNode> Units, List<CallGraphNode> Constructors) BuildAll()
        {
            var units = new List<CallGraphNode>();
            var ctors = new List<CallGraphNode>();

            void AddUnits(IEnumerable<string> classNames, string kind, Func<MethodStructureInfo, bool> filter)
            {
                foreach (var name in classNames.OrderBy(x => x, StringComparer.Ordinal))
                {
                    var cls = _ctx.Class(name)!;
                    foreach (var m in cls.Methods.Where(m => DomainContext.IsPublic(m.MethodDeclaration) && filter(m)))
                    {
                        var node = Trace(cls, m.MethodDeclaration, new List<SyntaxNode>());
                        node.UnitKind = kind;
                        units.Add(node);
                        if (!_unitsByClass.TryGetValue(name, out var ofClass)) _unitsByClass[name] = ofClass = new List<CallGraphNode>();
                        ofClass.Add(node);
                    }
                }
            }

            AddUnits(_ctx.Entities, "entity", m => !IsPlainGetter(m));
            AddUnits(_ctx.DomainServices, "domain service", _ => true);
            AddUnits(_ctx.FactoryClasses, "factory", _ => true);
            AddUnits(_ctx.Repositories, "repository", _ => true);

            foreach (var name in _ctx.Entities.OrderBy(x => x, StringComparer.Ordinal))
            {
                var cls = _ctx.Class(name)!;
                foreach (var ctor in cls.ClassDeclaration.Members.OfType<ConstructorDeclarationSyntax>())
                {
                    if (ctor.Body == null && ctor.ExpressionBody == null) continue;
                    var node = Trace(cls, ctor, new List<SyntaxNode>());
                    node.UnitKind = "constructor";
                    ctors.Add(node);
                }
            }
            return (units, ctors);
        }

        /// <summary>Method yang stereotype-nya hanya "get" (atau kosong) bukan perilaku domain.</summary>
        private static bool IsPlainGetter(MethodStructureInfo m) =>
            m.Stereotype.Count > 0 && m.Stereotype.All(s => s is MethodStereotype.Get or MethodStereotype.Empty);

        private CallGraphNode Trace(ClassStructureInfo cls, BaseMethodDeclarationSyntax decl, List<SyntaxNode> stack)
        {
            bool isEntity = _ctx.IsEntity(cls.ClassName);
            var node = new CallGraphNode(cls.ClassName, cls.ClassDeclaration, isEntity)
            {
                Method = decl,
                IsConstructor = decl is ConstructorDeclarationSyntax,
                MethodName = decl is MethodDeclarationSyntax md ? md.Identifier.Text : ".ctor"
            };

            if (stack.Contains(decl)) { node.IsRecursive = true; return node; }
            stack = new List<SyntaxNode>(stack) { decl };

            var sm = _ctx.Model(decl.SyntaxTree);
            var own = OwnUsage(decl);   // [EKSTENSI] pemakaian langsung method ini (sebelum info anak naik)

            // method entity selalu menyentuh entity-nya sendiri
            if (isEntity)
            {
                node.AccessedEntities.Add(cls.ClassName);
                own.Entities.Add(cls.ClassName);
            }

            // parameter bertipe entity
            foreach (var p in decl.ParameterList.Parameters)
            {
                node.AccessedEntities.UnionWith(_ctx.EntityNamesIn(p.Type));
                Use(own, p.Type);
            }

            // [EKSTENSI] tipe kembalian ikut dihitung sebagai "diakses" (tidak memengaruhi transaksi Fase 3)
            Use(own, (decl as MethodDeclarationSyntax)?.ReturnType);

            // perubahan state sendiri: pakai fakta & stereotype yang sudah dihitung (Stereocode)
            if (isEntity && !node.IsConstructor && _ctx.MethodInfo(decl) is { Facts: { } f } msi)
            {
                bool mutator = msi.Stereotype.Any(s => s.IsMutator());
                if (mutator || f.DataMembersModified > 0)
                {
                    node.DefinedEntities.Add(cls.ClassName);
                    node.ChangedMembers.UnionWith(f.ModifiedDataMembers);
                }
            }

            SyntaxNode? body = (SyntaxNode?)decl.Body ?? decl.ExpressionBody;
            if (body != null)
            {
                foreach (var n in body.DescendantNodes())
                {
                    switch (n)
                    {
                        case ObjectCreationExpressionSyntax:
                        case ImplicitObjectCreationExpressionSyntax:
                            HandleCreation(node, own, cls, (ExpressionSyntax)n, sm);
                            break;

                        case InvocationExpressionSyntax inv:
                            HandleInvocation(node, own, cls, inv, sm, stack);
                            break;

                        case IdentifierNameSyntax id:
                            {
                                // variabel / properti / parameter bertipe entity (dan VO) yang dipakai
                                var idType = sm.GetTypeInfo(id).Type;
                                node.AccessedEntities.UnionWith(_ctx.EntityNamesIn(idType));
                                Use(own, idType);
                                break;
                            }
                    }
                }
            }

            // info anak ikut "naik" ke induk (seperti log paper)
            foreach (var c in node.CalledMethods)
            {
                node.AccessedEntities.UnionWith(c.AccessedEntities);
                node.DefinedEntities.UnionWith(c.DefinedEntities);
                node.CreatedEntities.UnionWith(c.CreatedEntities);
            }

            // [EKSTENSI] factory class yang membuat beberapa entity: produk → entity lain
            if (_opt.FactoryCreationAsOwnership && _ctx.FactoryClasses.Contains(cls.ClassName) &&
                decl is MethodDeclarationSyntax fm && node.CreatedEntities.Count > 1)
            {
                var product = _ctx.EntityNamesIn(fm.ReturnType).FirstOrDefault(node.CreatedEntities.Contains);
                if (product != null)
                    foreach (var other in node.CreatedEntities.Where(x => x != product))
                        node.CreationRecords.Add(new CreationRecord(product, other, node.FullName, FromFactory: true));
            }
            return node;
        }

        private void HandleCreation(CallGraphNode node, MethodUsage own, ClassStructureInfo cls, ExpressionSyntax creation, SemanticModel sm)
        {
            string? name = sm.GetTypeInfo(creation).Type?.Name;
            if (string.IsNullOrEmpty(name) && creation is ObjectCreationExpressionSyntax oc)
                name = DomainContext.TypeNames(oc.Type).FirstOrDefault();
            if (string.IsNullOrEmpty(name)) return;

            if (_ctx.IsValueObject(name)) { own.ValueObjects.Add(name); return; }   // [EKSTENSI]
            if (!_ctx.IsEntity(name)) return;

            node.CreatedEntities.Add(name);
            node.AccessedEntities.Add(name);
            own.Entities.Add(name);

            // entity A memanggil constructor entity B → CreationRecord (paper 4.2.1)
            if (node.IsEntityMethod && name != cls.ClassName)
                node.CreationRecords.Add(new CreationRecord(cls.ClassName, name, node.FullName));
        }

        private void HandleInvocation(CallGraphNode node, MethodUsage own, ClassStructureInfo cls, InvocationExpressionSyntax inv,
                                      SemanticModel sm, List<SyntaxNode> stack)
        {
            string? targetClass = null;
            MethodDeclarationSyntax? targetDecl = null;

            // 1) SemanticModel: akurat, termasuk overload
            var info = sm.GetSymbolInfo(inv);
            var ms = info.Symbol as IMethodSymbol ?? info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
            if (ms != null)
            {
                targetClass = ms.ContainingType?.Name;
                targetDecl = ms.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<MethodDeclarationSyntax>().FirstOrDefault();
                Use(own, ms.ReturnType);   // [EKSTENSI] mis. order.Total() → Money ikut "dipakai"
            }

            // 2) cadangan: tipe penerima + nama method (overload pertama)
            if (targetClass == null || targetDecl == null)
            {
                string? methodName = null;
                switch (inv.Expression)
                {
                    case MemberAccessExpressionSyntax ma:
                        targetClass ??= sm.GetTypeInfo(ma.Expression).Type?.Name;
                        methodName = ma.Name.Identifier.Text;
                        break;
                    case IdentifierNameSyntax id:
                        targetClass ??= cls.ClassName;
                        methodName = id.Identifier.Text;
                        break;
                }
                if (targetClass != null && methodName != null && _ctx.Class(targetClass) is { } tc)
                    targetDecl ??= tc.Methods.FirstOrDefault(m => m.MethodName == methodName)?.MethodDeclaration;
            }

            if (targetClass == null) return;
            if (!_ctx.IsDomainClass(targetClass))
            {
                // [EKSTENSI] lewat interface (DI): catat class implementasinya sebagai domain class yang dipakai.
                // Call graph TIDAK diteruskan ke implementasi, jadi transaksi Fase 3 tidak berubah.
                if (ms?.ContainingType is { TypeKind: TypeKind.Interface } iface)
                    foreach (var impl in _ctx.ImplementationsOf(iface))
                        if (impl != cls.ClassName) own.DomainClasses.Add(impl);
                return;   // di luar domain → berhenti
            }

            if (_ctx.IsEntity(targetClass))
            {
                node.AccessedEntities.Add(targetClass);
                own.Entities.Add(targetClass);
            }
            else if (_ctx.IsValueObject(targetClass)) own.ValueObjects.Add(targetClass);   // [EKSTENSI]
            else if (targetClass != cls.ClassName) own.DomainClasses.Add(targetClass);     // [EKSTENSI]

            var targetCls = _ctx.Class(targetClass)!;
            if (targetDecl != null && targetCls.ClassDeclaration.Members.Contains(targetDecl))
                node.CalledMethods.Add(Trace(targetCls, targetDecl, stack));
        }
    }

    internal static class DomainModelAccessResolver
    {
        private const string RepositoryKind = "repository";
        private const string DomainServiceKind = "domain service";

        /// <summary>Fase 1: daftar domain model yang diakses, dari pemakaian per method di call graph.</summary>
        public static List<DomainModelAccessInfo> FillAccessedModels(
            DomainModelList dms, DomainContext ctx, CallGraphBuilder cg, AggregateIdentifierOptions opt)
        {
            var result = new List<DomainModelAccessInfo>();

            foreach (var dm in dms.DomainModels)
            {
                switch (dm)
                {
                    case DomainService s when ctx.DomainServices.Contains(s.Name):
                        {
                            var usage = Collect(s.Name, ctx, cg, opt.TransitiveDomainModelAccess);
                            s.DomainModelInvolved.Clear();
                            foreach (var target in Resolve(dms, usage, includeDomainClasses: true, self: s))
                                s.AddDomainModelInvolved(target);
                            result.Add(Info(s.Name, DomainServiceKind, null, usage));
                            break;
                        }
                    case Repository r when ctx.Repositories.Contains(r.Name):
                        {
                            var usage = Collect(r.Name, ctx, cg, opt.TransitiveDomainModelAccess);
                            // sesuai komentar di Repository: hanya entity / value object (/ aggregate)
                            r.AccessedDomainModels.Clear();
                            foreach (var target in Resolve(dms, usage, includeDomainClasses: false, self: r))
                                r.AddAccessedDomainModel(target);
                            result.Add(Info(r.Name, RepositoryKind, null, usage));
                            break;
                        }
                }
            }

            return result
                .GroupBy(a => (a.Kind, a.Owner)).Select(g => g.First())   // nama ganda di DomainModelList
                .OrderBy(a => a.Kind, StringComparer.Ordinal)
                .ThenBy(a => a.Owner, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Fase 2: tipe utama repository. Urutan: nama cocok (OrderRepository → Order) → entity lebih dulu
        /// daripada VO → paling sering muncul di signature → nama. Kandidat diambil dari signature, ditambah
        /// hasil Fase 1 sebagai cadangan bila signature tidak menyebut entity/VO sama sekali.
        /// </summary>
        public static void FillRepositoryType(DomainModelList dms, DomainContext ctx, List<DomainModelAccessInfo> access)
        {
            for (int i = 0; i < access.Count; i++)
            {
                var a = access[i];
                if (a.Kind != RepositoryKind) continue;

                var hits = ctx.RepositorySignatureHits(a.Owner);
                string? primary = hits.Keys.Concat(a.Entities).Concat(a.ValueObjects)
                    .Distinct(StringComparer.Ordinal)
                    .OrderByDescending(t => NameMatch(a.Owner, t))
                    .ThenByDescending(t => ctx.IsEntity(t))
                    .ThenByDescending(t => hits.GetValueOrDefault(t))
                    .ThenBy(t => t, StringComparer.Ordinal)
                    .FirstOrDefault();

                access[i] = a with { PrimaryType = primary };
                foreach (var r in dms.DomainModels.OfType<Repository>().Where(r => r.Name == a.Owner))
                    r.AccessedType = primary;
            }
        }

        // ---------------------------------------------------------------- bantuan

        /// <summary>
        /// Langsung  : gabungan pemakaian method-method milik class itu sendiri (publik + helper privat
        ///             yang terjangkau), tanpa isi method class lain.
        /// Transitif : gabungan pemakaian semua node di call graph unit-unit class itu.
        /// </summary>
        private static MethodUsage Collect(string owner, DomainContext ctx, CallGraphBuilder cg, bool transitive)
        {
            IEnumerable<SyntaxNode> decls = transitive
                ? cg.UnitsOf(owner).SelectMany(u => u.Walk()).Where(n => n.Method != null).Select(n => (SyntaxNode)n.Method!)
                : ctx.Class(owner)!.Methods.Select(m => (SyntaxNode)m.MethodDeclaration);

            var acc = new MethodUsage();
            foreach (var d in decls)
                if (cg.OwnUsageOf(d) is { } u) acc.UnionWith(u);
            acc.DomainClasses.Remove(owner);
            return acc;
        }

        /// <summary>Nama → objek DomainModelStruct, dengan jenis yang cocok (entity hanya dipetakan ke Entity, dst.).</summary>
        private static IEnumerable<DomainModelStruct> Resolve(
            DomainModelList dms, MethodUsage u, bool includeDomainClasses, DomainModelStruct self)
        {
            foreach (var dm in dms.DomainModels)
            {
                if (ReferenceEquals(dm, self)) continue;
                bool hit = dm switch
                {
                    Entity => u.Entities.Contains(dm.Name),
                    ValueObject => u.ValueObjects.Contains(dm.Name),
                    DomainService or Repository => includeDomainClasses && u.DomainClasses.Contains(dm.Name),
                    Factory f => includeDomainClasses && f.IsClass && u.DomainClasses.Contains(dm.Name),
                    _ => false
                };
                if (hit) yield return dm;
            }
        }

        private static DomainModelAccessInfo Info(string owner, string kind, string? primary, MethodUsage u) =>
            new(owner, kind, primary, Sorted(u.Entities), Sorted(u.ValueObjects), Sorted(u.DomainClasses));

        private static IReadOnlyList<string> Sorted(IEnumerable<string> xs) =>
            xs.OrderBy(x => x, StringComparer.Ordinal).ToList();

        /// <summary>2 = nama repository persis menunjuk tipe ini, 1 = berakhiran nama tipe (EfOrderRepository), 0 = tidak.</summary>
        private static int NameMatch(string repository, string type)
        {
            string stem = repository;
            if (stem.Length > 1 && stem[0] == 'I' && char.IsUpper(stem[1])) stem = stem[1..];
            foreach (var suffix in new[] { "Repository", "Repo" })
                if (stem.EndsWith(suffix, StringComparison.Ordinal)) { stem = stem[..^suffix.Length]; break; }
            if (stem.Length == 0) return 0;

            var forms = new List<string> { type, type + "s", type + "es" };
            if (type.EndsWith("y", StringComparison.Ordinal)) forms.Add(type[..^1] + "ies");

            if (forms.Contains(stem)) return 2;
            if (forms.Any(f => stem.EndsWith(f, StringComparison.Ordinal))) return 1;
            return 0;
        }
    }

    // =====================================================================================
    //  FASE 2 — ClusteringGraph awal
    // =====================================================================================

    internal static class ClusteringGraphInitializer
    {
        public static ClusteringGraph Build(DomainContext ctx, AggregateIdentifierOptions opt,
                                            out List<IdReference> idRefs, out List<EntityReference> entityRefs)
        {
            var g = new ClusteringGraph();
            idRefs = new List<IdReference>();
            entityRefs = new List<EntityReference>();
            var entities = ctx.Entities.OrderBy(x => x, StringComparer.Ordinal).ToList();
            foreach (var e in entities) g.AddNode(e);

            var refs = new List<(string From, string To, string Reason)>();

            foreach (var e in entities)
            {
                foreach (var (memberName, type) in StateMembers(ctx.Class(e)!.ClassDeclaration))
                {
                    foreach (var t in DomainContext.TypeNames(type).Distinct())
                    {
                        string vnode = opt.ShareValueObjectNodes ? t : DomainContext.OwnedNode(t, e);
                        if (ctx.Enums.Contains(t))
                            g.SetEdge(e, vnode, EdgeType.Value, $"{e}.{memberName} bertipe enum");
                        else if (ctx.IsValueObject(t))
                            g.SetEdge(e, vnode, EdgeType.Embedded, $"{e}.{memberName} bertipe value object");
                        else if (ctx.IsEntity(t) && t != e)
                        {
                            refs.Add((e, t, $"{e}.{memberName}"));
                            entityRefs.Add(new EntityReference(e, t, memberName, ViaId: false));
                        }
                    }

                    // referensi lewat ID: ResidenceId, OrderLineIds, _orderLineId, ...
                    var idName = memberName.TrimStart('_');
                    if (idName.Length > 0) idName = char.ToUpperInvariant(idName[0]) + idName[1..];
                    var m = Regex.Match(idName, "^(.+?)Ids?$");
                    if (m.Success && ctx.IsEntity(m.Groups[1].Value) && m.Groups[1].Value != e)
                    {
                        idRefs.Add(new IdReference(e, m.Groups[1].Value, memberName));
                        entityRefs.Add(new EntityReference(e, m.Groups[1].Value, memberName, ViaId: true));
                    }
                }

                if (opt.MethodParameterAsReference)
                    foreach (var mi in ctx.Class(e)!.Methods.Where(x => DomainContext.IsPublic(x.MethodDeclaration)))
                        foreach (var p in mi.MethodDeclaration.ParameterList.Parameters)
                            foreach (var t in ctx.EntityNamesIn(p.Type).Where(t => t != e))
                                refs.Add((e, t, $"argumen {e}.{mi.MethodName}({p.Identifier.Text})"));
            }

            // REFERENCE hanya bila pasangan itu belum punya edge lain (seperti AssociationDetection)
            foreach (var r in refs)
                if (g.GetEdge(r.From, r.To) == null)
                    g.SetEdge(r.From, r.To, EdgeType.Reference, r.Reason);

            return g;
        }

        /// <summary>Field & properti instance (bukan static/const), termasuk properti terhitung.</summary>
        private static IEnumerable<(string Name, TypeSyntax Type)> StateMembers(ClassDeclarationSyntax cls)
        {
            foreach (var member in cls.Members)
            {
                switch (member)
                {
                    case FieldDeclarationSyntax f when !f.Modifiers.Any(SyntaxKind.StaticKeyword) && !f.Modifiers.Any(SyntaxKind.ConstKeyword):
                        foreach (var v in f.Declaration.Variables)
                            yield return (v.Identifier.Text, f.Declaration.Type);
                        break;
                    case PropertyDeclarationSyntax p when !p.Modifiers.Any(SyntaxKind.StaticKeyword):
                        yield return (p.Identifier.Text, p.Type);
                        break;
                }
            }
        }
    }

    // =====================================================================================
    //  FASE 3 — Graph enhancement
    // =====================================================================================

    internal static class GraphEnhancer
    {
        public sealed record Result(List<string> OwnershipLog, List<CouplingScore> Scores, double Threshold);

        public static Result Enhance(ClusteringGraph g, List<CallGraphNode> units, List<CallGraphNode> ctors)
        {
            // ---- Langkah 1: OWNERSHIP dari CreationRecord (method + constructor entity) ----
            var log = new List<string>();
            var seen = new HashSet<(string, string)>();
            foreach (var rec in units.Concat(ctors).SelectMany(cg => cg.Walk()).SelectMany(n => n.CreationRecords))
            {
                if (!seen.Add((rec.Creator, rec.Created))) continue;
                var old = g.GetEdge(rec.Creator, rec.Created);
                g.SetEdge(rec.Creator, rec.Created, EdgeType.Ownership,
                          $"{rec.Creator} membuat {rec.Created} di {rec.FoundIn}", owner: rec.Creator);
                log.Add($"{rec.Creator} → {rec.Created}: {(old?.Type.ToText() ?? "(tidak ada)")} ⇒ OWNERSHIP (2.0)   " +
                        $"[ditemukan di {rec.FoundIn}{(rec.FromFactory ? ", lewat factory" : "")}]");
            }

            // ---- Langkah 2: coupling score (1 unit = 1 transaksi) ----
            var tx = units.Select(u => u.AllEntities()).ToList();
            var total = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var t in tx)
                foreach (var e in t)
                    total[e] = total.GetValueOrDefault(e) + 1;

            var refEdges = g.Edges.Where(e => e.Type == EdgeType.Reference).ToList();
            var raw = new List<(ClusterEdge Edge, int Both, int TA, int TB, double Score)>();
            foreach (var e in refEdges)
            {
                int both = tx.Count(t => t.Contains(e.A) && t.Contains(e.B));
                int ta = total.GetValueOrDefault(e.A), tb = total.GetValueOrDefault(e.B);
                double score = ta + tb == 0 ? 0 : 2.0 * both / (ta + tb);
                raw.Add((e, both, ta, tb, score));
            }

            double threshold = raw.Count == 0 ? 0 : raw.Average(r => r.Score);
            var scores = new List<CouplingScore>();
            foreach (var r in raw.OrderByDescending(r => r.Score).ThenBy(r => r.Edge.A).ThenBy(r => r.Edge.B))
            {
                bool promote = r.Score > threshold;
                if (promote)
                {
                    r.Edge.Type = EdgeType.Coupled;
                    r.Edge.Weight = EdgeType.Coupled.Baseline();
                }
                else r.Edge.Weight = Math.Round(r.Score, 3);
                scores.Add(new CouplingScore(r.Edge.A, r.Edge.B, r.Both, r.TA, r.TB, r.Score, promote));
            }
            return new Result(log, scores, threshold);
        }
    }

    // =====================================================================================
    //  FASE 4 — Strict Aggregate Clustering (DFS + Union-Find)
    // =====================================================================================

    internal static class StrictAggregateClustering
    {
        public static List<HashSet<string>> Cluster(ClusteringGraph g)
        {
            var parent = g.Nodes.ToDictionary(n => n, n => n, StringComparer.Ordinal);

            string Find(string x)
            {
                while (parent[x] != x)
                {
                    parent[x] = parent[parent[x]];   // path halving
                    x = parent[x];
                }
                return x;
            }
            void Union(string a, string b)
            {
                string ra = Find(a), rb = Find(b);
                if (ra != rb) parent[rb] = ra;
            }

            // DFS iteratif: hanya mengikuti edge kuat
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var start in g.Nodes)
            {
                if (visited.Contains(start)) continue;
                var stack = new Stack<string>();
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var v = stack.Pop();
                    if (!visited.Add(v)) continue;
                    foreach (var e in g.EdgesOf(v).Where(e => e.Type.IsStrong()))
                    {
                        var u = e.Other(v);
                        Union(v, u);
                        if (!visited.Contains(u)) stack.Push(u);
                    }
                }
            }

            return g.Nodes.GroupBy(Find).Select(gr => gr.ToHashSet(StringComparer.Ordinal)).ToList();
        }
    }
}