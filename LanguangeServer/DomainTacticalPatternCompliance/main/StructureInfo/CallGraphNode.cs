using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainTacticalPatternCompliance.Main.StructureInfo
{
    /// <summary>
    /// "Entity A memanggil constructor entity B" (Iliopoulos 2025, 4.2.1). Bahan edge OWNERSHIP.
    /// </summary>
    public readonly record struct CreationRecord(string Creator, string Created, string FoundIn, bool FromFactory = false);

    /// <summary>
    /// Satu node call graph = satu method (atau constructor) yang dikunjungi.
    /// Root sebuah call graph adalah "unit perilaku domain" (pengganti endpoint di paper).
    /// </summary>
    public class CallGraphNode
    {
        // ---- field asli ----
        public string ClassName;
        public ClassDeclarationSyntax ClassDeclaration;
        public bool IsEntityMethod;

        // ---- identitas method ----
        public string MethodName = "";
        public BaseMethodDeclarationSyntax? Method;
        public bool IsConstructor;
        /// <summary>Jenis unit untuk root: "entity", "domain service", "factory", "repository", "constructor".</summary>
        public string UnitKind = "";
        /// <summary>Method sudah ada di jalur pemanggilan → tidak ditelusuri lagi (cegah rekursi tak berujung).</summary>
        public bool IsRecursive;

        // ---- isi seperti CallGraphNode di paper ----
        public HashSet<string> AccessedEntities { get; } = new();
        public HashSet<string> DefinedEntities { get; } = new();
        public HashSet<string> CreatedEntities { get; } = new();
        public HashSet<string> ChangedMembers { get; } = new();
        public List<CreationRecord> CreationRecords { get; } = new();
        public List<CallGraphNode> CalledMethods { get; } = new();

        public bool IsReadOnly => DefinedEntities.Count == 0 && CreatedEntities.Count == 0;
        public string FullName => $"{ClassName}.{MethodName}";

        public CallGraphNode(string className, ClassDeclarationSyntax classDeclaration, bool isEntityMethod)
        {
            ClassName = className;
            ClassDeclaration = classDeclaration;
            IsEntityMethod = isEntityMethod;
        }

        /// <summary>Node ini beserta semua turunannya (pre-order).</summary>
        public IEnumerable<CallGraphNode> Walk()
        {
            yield return this;
            foreach (var child in CalledMethods)
                foreach (var n in child.Walk())
                    yield return n;
        }

        /// <summary>Semua entity yang terlibat di call graph ini = satu "transaksi" untuk coupling score.</summary>
        public HashSet<string> AllEntities()
        {
            var all = new HashSet<string>();
            foreach (var n in Walk())
            {
                all.UnionWith(n.AccessedEntities);
                all.UnionWith(n.DefinedEntities);
                all.UnionWith(n.CreatedEntities);
            }
            return all;
        }

        /// <summary>Gambar pohon seperti log tool di paper (Figure 23).</summary>
        public void Print(StringBuilder sb, string prefix = "", string childPrefix = "")
        {
            var tags = new List<string>();
            if (IsEntityMethod) tags.Add("Entity method");
            if (IsReadOnly) tags.Add("ReadOnly");
            if (ChangedMembers.Count > 0) tags.Add("Changes: " + string.Join(", ", ChangedMembers.OrderBy(x => x)));
            if (IsRecursive) tags.Add("rekursif, berhenti");
            sb.AppendLine($"{prefix}{FullName}{string.Concat(tags.Select(t => $" [{t}]"))}");

            string pad = childPrefix + (CalledMethods.Count > 0 ? "│     " : "      ");
            if (DefinedEntities.Count > 0) sb.AppendLine($"{pad}defined  = [{string.Join(", ", DefinedEntities.OrderBy(x => x))}]");
            if (CreatedEntities.Count > 0) sb.AppendLine($"{pad}created  = [{string.Join(", ", CreatedEntities.OrderBy(x => x))}]");
            foreach (var r in CreationRecords)
                sb.AppendLine($"{pad}CreationRecord: {r.Creator} → {r.Created}{(r.FromFactory ? " (lewat factory)" : "")}   ← bahan OWNERSHIP");

            for (int i = 0; i < CalledMethods.Count; i++)
            {
                bool last = i == CalledMethods.Count - 1;
                CalledMethods[i].Print(sb, childPrefix + (last ? "└── " : "├── "), childPrefix + (last ? "    " : "│   "));
            }
        }
    }
}
