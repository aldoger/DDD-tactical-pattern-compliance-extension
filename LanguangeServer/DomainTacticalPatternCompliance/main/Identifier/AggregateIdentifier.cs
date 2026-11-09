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
    //   Fase 4  Clustering                 : SAC (ketat) dan Louvain (longgar)
    //
    //  Bagian yang BUKAN dari paper ditandai [EKSTENSI].
    // =====================================================================================

    public enum ClusteringAlgorithm { Strict, Louvain }

    public sealed class AggregateIdentifierOptions
    {
        /// <summary>Berapa kali Louvain dijalankan; konfigurasi yang paling sering muncul dipilih.</summary>
        public int LouvainRuns { get; init; } = 100;

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
    }

    public readonly record struct CouplingScore(
        string A, string B, int Together, int TotalA, int TotalB, double Score, bool Promoted);

    public readonly record struct IdReference(string FromEntity, string ToEntity, string Member);

    public sealed class AggregateCandidate
    {
        public string? Root { get; init; }
        public string RootReason { get; init; } = "";
        public List<string> Entities { get; init; } = new();
        public List<string> ValueObjects { get; init; } = new();
        public List<string> Enums { get; init; } = new();
        public List<string> Warnings { get; init; } = new();
        public IEnumerable<string> Members => Entities.Concat(ValueObjects).Concat(Enums);

        /// <summary>Tipe tiap anggota: root → AGGREGATE_ROOT, anggota lain → AGGREGATE_PART. Kosong bila cluster tanpa root.</summary>
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
        public List<AggregateCandidate> LouvainAggregates { get; init; } = new();
        public int LouvainRuns { get; init; }
        public int LouvainFrequency { get; init; }
        public int LouvainDistinctConfigurations { get; init; }

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

            Header("FASE 2 — ClusteringGraph awal");
            sb.AppendLine($"Node: [{string.Join(", ", InitialGraph.Nodes)}]");
            foreach (var e in InitialGraph.Edges.OrderBy(e => e.Type).ThenBy(e => e.A).ThenBy(e => e.B))
                sb.AppendLine($"  {e.Type.ToText(),-10} {e.A} — {e.B}   ({e.Reason})");
            if (IdReferences.Count > 0)
            {
                sb.AppendLine("Referensi lewat ID (dicatat, BUKAN edge — DDD menganjurkan antar-aggregate lewat ID):");
                foreach (var r in IdReferences) sb.AppendLine($"  {r.FromEntity}.{r.Member} → {r.ToEntity}");
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

            Header("FASE 4a — Strict Aggregate Clustering (SAC)");
            PrintCandidates(sb, StrictAggregates);
            Header($"FASE 4b — Louvain Clustering ({LouvainRuns} run)");
            sb.AppendLine($"Konfigurasi terpilih muncul {LouvainFrequency}/{LouvainRuns} kali ({LouvainDistinctConfigurations} konfigurasi berbeda).");
            PrintCandidates(sb, LouvainAggregates);
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

        /// <param name="compilation">Compilation yang memuat SEMUA syntax tree yang dianalisis (lihat Parser.Compilation).</param>
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

            // Fase 2
            var graph = ClusteringGraphInitializer.Build(ctx, _options, out var idRefs);
            var initial = graph.Clone();

            // Fase 3
            var enh = GraphEnhancer.Enhance(graph, units, ctors);

            // Fase 4
            var sac = StrictAggregateClustering.Cluster(graph);
            var lv = LouvainClustering.ClusterRepeated(graph, _options.LouvainRuns);

            return new AggregateIdentificationResult
            {
                CallGraphs = units,
                ConstructorGraphs = ctors,
                InitialGraph = initial,
                EnhancedGraph = graph,
                OwnershipLog = enh.OwnershipLog,
                CouplingScores = enh.Scores,
                DynamicThreshold = enh.Threshold,
                IdReferences = idRefs,
                StrictAggregates = sac.Select(c => Describe(c, graph, ctx)).OrderByDescending(a => a.Members.Count()).ThenBy(a => a.Root).ToList(),
                LouvainAggregates = lv.Clusters.Select(c => Describe(c, graph, ctx)).OrderByDescending(a => a.Members.Count()).ThenBy(a => a.Root).ToList(),
                LouvainRuns = _options.LouvainRuns,
                LouvainFrequency = lv.Frequency,
                LouvainDistinctConfigurations = lv.Distinct
            };
        }

        /// <summary>
        /// Daftarkan hasil ke DomainModelList sebagai objek Aggregate (root / part).
        /// Value object ikut didaftarkan sebagai part (C18). Enum tidak punya ClassStructureInfo, jadi dilewati.
        /// </summary>
        public void RegisterAggregates(DomainModelList dms, AggregateIdentificationResult result,
                                       ClusteringAlgorithm algorithm = ClusteringAlgorithm.Strict)
        {
            var ctx = new DomainContext(dms, _compilation);
            var list = algorithm == ClusteringAlgorithm.Strict ? result.StrictAggregates : result.LouvainAggregates;

            foreach (var cand in list.Where(c => c.Root != null))
            {
                foreach (var name in cand.Entities.Concat(cand.ValueObjects))
                {
                    var info = ctx.Class(name);
                    if (info == null) continue;
                    var agg = new Aggregate(name, info);
                    agg.SetAggregateType(cand.TypeOf(name) ?? AggregateType.AGGREGATE_PART);
                    agg.SetRootName(cand.Root!);
                    dms.AddDomainModel(agg);
                }
            }
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

                // [EKSTENSI] validasi dengan repository: BUKAN dipakai membentuk cluster, hanya diperiksa
                foreach (var e in entities)
                {
                    var repos = ctx.RepositoriesAccessing(e);
                    if (e == root && repos.Count == 0)
                        warnings.Add($"root {e} tidak diakses repository mana pun (periksa C23)");
                    if (e != root && repos.Count > 0)
                        warnings.Add($"entity bagian {e} punya repository sendiri ({string.Join(", ", repos)}) → kandidat pelanggaran C23 / batas aggregate keliru");
                }
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

        public HashSet<string> Entities { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ValueObjects { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Enums { get; } = new(StringComparer.Ordinal);
        public HashSet<string> DomainServices { get; } = new(StringComparer.Ordinal);
        public HashSet<string> FactoryClasses { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Repositories { get; } = new(StringComparer.Ordinal);

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

            // enum belum masuk DomainModelList (GetClasses hanya class) → ambil dari syntax tree
            foreach (var tree in compilation.SyntaxTrees)
                foreach (var en in tree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>())
                    Enums.Add(en.Identifier.Text);

            // repository → entity yang diaksesnya (tipe parameter & return), sama seperti C12
            foreach (var repo in Repositories)
                foreach (var m in _classes[repo].Methods)
                {
                    var names = m.MethodDeclaration.ParameterList.Parameters.SelectMany(p => TypeNames(p.Type))
                        .Concat(TypeNames(m.MethodDeclaration.ReturnType));
                    foreach (var n in names.Where(Entities.Contains))
                    {
                        if (!_repoAccess.TryGetValue(n, out var l)) _repoAccess[n] = l = new List<string>();
                        if (!l.Contains(repo)) l.Add(repo);
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

        /// <summary>Nama entity yang disebut dalam sebuah tipe simbol, termasuk argumen generic dan array.</summary>
        public IEnumerable<string> EntityNamesIn(ITypeSymbol? t)
        {
            switch (t)
            {
                case null: yield break;
                case IArrayTypeSymbol arr:
                    foreach (var n in EntityNamesIn(arr.ElementType)) yield return n;
                    break;
                case INamedTypeSymbol nt:
                    if (Entities.Contains(nt.Name)) yield return nt.Name;
                    foreach (var arg in nt.TypeArguments)
                        foreach (var n in EntityNamesIn(arg)) yield return n;
                    break;
            }
        }

        public IEnumerable<string> EntityNamesIn(TypeSyntax? t) => TypeNames(t).Where(Entities.Contains);

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

    internal sealed class CallGraphBuilder
    {
        private readonly DomainContext _ctx;
        private readonly AggregateIdentifierOptions _opt;

        public CallGraphBuilder(DomainContext ctx, AggregateIdentifierOptions opt)
        {
            _ctx = ctx; _opt = opt;
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

            // method entity selalu menyentuh entity-nya sendiri
            if (isEntity) node.AccessedEntities.Add(cls.ClassName);

            // parameter bertipe entity
            foreach (var p in decl.ParameterList.Parameters)
                node.AccessedEntities.UnionWith(_ctx.EntityNamesIn(p.Type));

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
                            HandleCreation(node, cls, (ExpressionSyntax)n, sm);
                            break;

                        case InvocationExpressionSyntax inv:
                            HandleInvocation(node, cls, inv, sm, stack);
                            break;

                        case IdentifierNameSyntax id:
                            // variabel / properti / parameter bertipe entity yang dipakai
                            node.AccessedEntities.UnionWith(_ctx.EntityNamesIn(sm.GetTypeInfo(id).Type));
                            break;
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

        private void HandleCreation(CallGraphNode node, ClassStructureInfo cls, ExpressionSyntax creation, SemanticModel sm)
        {
            string? name = sm.GetTypeInfo(creation).Type?.Name;
            if (string.IsNullOrEmpty(name) && creation is ObjectCreationExpressionSyntax oc)
                name = DomainContext.TypeNames(oc.Type).FirstOrDefault();
            if (name == null || !_ctx.IsEntity(name)) return;

            node.CreatedEntities.Add(name);
            node.AccessedEntities.Add(name);

            // entity A memanggil constructor entity B → CreationRecord (paper 4.2.1)
            if (node.IsEntityMethod && name != cls.ClassName)
                node.CreationRecords.Add(new CreationRecord(cls.ClassName, name, node.FullName));
        }

        private void HandleInvocation(CallGraphNode node, ClassStructureInfo cls, InvocationExpressionSyntax inv,
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

            if (targetClass == null || !_ctx.IsDomainClass(targetClass)) return;   // di luar domain → berhenti
            if (_ctx.IsEntity(targetClass)) node.AccessedEntities.Add(targetClass);

            var targetCls = _ctx.Class(targetClass)!;
            if (targetDecl != null && targetCls.ClassDeclaration.Members.Contains(targetDecl))
                node.CalledMethods.Add(Trace(targetCls, targetDecl, stack));
        }
    }

    // =====================================================================================
    //  FASE 2 — ClusteringGraph awal
    // =====================================================================================

    internal static class ClusteringGraphInitializer
    {
        public static ClusteringGraph Build(DomainContext ctx, AggregateIdentifierOptions opt, out List<IdReference> idRefs)
        {
            var g = new ClusteringGraph();
            idRefs = new List<IdReference>();
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
                            refs.Add((e, t, $"{e}.{memberName}"));
                    }

                    // referensi lewat ID: ResidenceId, OrderLineIds, ...
                    var m = Regex.Match(memberName, "^(.+?)Ids?$");
                    if (m.Success && ctx.IsEntity(m.Groups[1].Value) && m.Groups[1].Value != e)
                        idRefs.Add(new IdReference(e, m.Groups[1].Value, memberName));
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
    //  FASE 4a — Strict Aggregate Clustering (DFS + Union-Find)
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

    // =====================================================================================
    //  FASE 4b — Louvain (Blondel et al., 2008), tanpa library eksternal
    // =====================================================================================

    internal static class LouvainClustering
    {
        public sealed record Result(List<HashSet<string>> Clusters, int Frequency, int Distinct);

        /// <summary>Jalankan beberapa kali dengan urutan node acak; pilih konfigurasi yang paling sering (paper 4.2.4).</summary>
        public static Result ClusterRepeated(ClusteringGraph g, int runs)
        {
            var names = g.Nodes.ToList();
            var index = names.Select((n, i) => (n, i)).ToDictionary(x => x.n, x => x.i, StringComparer.Ordinal);

            var tally = new Dictionary<string, (int Count, List<HashSet<string>> Clusters)>();
            for (int seed = 0; seed < Math.Max(1, runs); seed++)
            {
                var assignment = RunOnce(names.Count, g.Edges.Select(e => (index[e.A], index[e.B], e.Weight)), new Random(seed));
                var clusters = assignment.Select((c, i) => (c, i)).GroupBy(x => x.c)
                    .Select(gr => gr.Select(x => names[x.i]).ToHashSet(StringComparer.Ordinal)).ToList();
                string key = string.Join(" | ", clusters.Select(c => string.Join(",", c.OrderBy(x => x, StringComparer.Ordinal)))
                                                         .OrderBy(x => x, StringComparer.Ordinal));
                tally[key] = tally.TryGetValue(key, out var t) ? (t.Count + 1, t.Clusters) : (1, clusters);
            }

            var best = tally.OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key, StringComparer.Ordinal).First();
            return new Result(best.Value.Clusters, best.Value.Count, tally.Count);
        }

        /// <summary>Satu run Louvain. Mengembalikan nomor komunitas untuk tiap node asli.</summary>
        private static int[] RunOnce(int n, IEnumerable<(int A, int B, double W)> edges, Random rnd)
        {
            var graph = new WGraph(n);
            foreach (var (a, b, w) in edges) graph.Add(a, b, w);

            var membership = Enumerable.Range(0, n).ToArray();   // node asli → node di level sekarang
            while (true)
            {
                var (comm, moved) = OneLevel(graph, rnd);
                if (!moved) break;
                for (int i = 0; i < n; i++) membership[i] = comm[membership[i]];
                graph = graph.Aggregate(comm);
            }
            return membership;
        }

        /// <summary>Fase local moving: pindahkan node ke komunitas tetangga dengan gain modularity terbesar.</summary>
        private static (int[] Comm, bool Moved) OneLevel(WGraph g, Random rnd)
        {
            int n = g.N;
            var comm = Enumerable.Range(0, n).ToArray();
            double m = g.TotalWeight;
            if (m <= 0) return (comm, false);

            var deg = Enumerable.Range(0, n).Select(g.Degree).ToArray();
            var tot = (double[])deg.Clone();
            bool anyMove = false, improved = true;

            while (improved)
            {
                improved = false;
                foreach (int i in Enumerable.Range(0, n).OrderBy(_ => rnd.Next()).ToList())
                {
                    int ci = comm[i];
                    var toComm = new Dictionary<int, double>();
                    foreach (var (j, w) in g.Neighbors(i))
                        if (j != i) toComm[comm[j]] = toComm.GetValueOrDefault(comm[j]) + w;

                    tot[ci] -= deg[i];
                    int best = ci;
                    double bestGain = toComm.GetValueOrDefault(ci) - tot[ci] * deg[i] / (2 * m);
                    foreach (var (c, w) in toComm)
                    {
                        double gain = w - tot[c] * deg[i] / (2 * m);
                        if (gain > bestGain + 1e-12) { bestGain = gain; best = c; }
                    }
                    tot[best] += deg[i];
                    comm[i] = best;
                    if (best != ci) { improved = true; anyMove = true; }
                }
            }

            // nomori ulang komunitas 0..k-1
            var renum = new Dictionary<int, int>();
            for (int i = 0; i < n; i++)
            {
                if (!renum.TryGetValue(comm[i], out var r)) renum[comm[i]] = r = renum.Count;
                comm[i] = r;
            }
            return (comm, anyMove && renum.Count < n);
        }

        /// <summary>Graf berbobot tak berarah sederhana; self-loop disimpan di adj[i][i].</summary>
        private sealed class WGraph
        {
            private readonly Dictionary<int, double>[] _adj;
            public int N => _adj.Length;

            public WGraph(int n)
            {
                _adj = new Dictionary<int, double>[n];
                for (int i = 0; i < n; i++) _adj[i] = new Dictionary<int, double>();
            }

            public void Add(int a, int b, double w)
            {
                if (w <= 0) return;
                _adj[a][b] = _adj[a].GetValueOrDefault(b) + w;
                if (a != b) _adj[b][a] = _adj[b].GetValueOrDefault(a) + w;
            }

            public IEnumerable<(int, double)> Neighbors(int i) => _adj[i].Select(kv => (kv.Key, kv.Value));

            /// <summary>Derajat berbobot; self-loop dihitung dua kali.</summary>
            public double Degree(int i) => _adj[i].Sum(kv => kv.Key == i ? 2 * kv.Value : kv.Value);

            /// <summary>Total bobot edge (setiap edge sekali).</summary>
            public double TotalWeight
            {
                get
                {
                    double s = 0;
                    for (int i = 0; i < N; i++)
                        foreach (var kv in _adj[i])
                            if (kv.Key >= i) s += kv.Value;
                    return s;
                }
            }

            /// <summary>Fase coarsening: satu komunitas menjadi satu node.</summary>
            public WGraph Aggregate(int[] comm)
            {
                var g = new WGraph(comm.Max() + 1);
                for (int i = 0; i < N; i++)
                    foreach (var kv in _adj[i])
                        if (kv.Key >= i) g.Add(comm[i], comm[kv.Key], kv.Value);
                return g;
            }
        }
    }
}
