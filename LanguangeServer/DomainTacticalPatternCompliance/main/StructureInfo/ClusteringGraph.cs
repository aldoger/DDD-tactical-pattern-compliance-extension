using System;
using System.Collections.Generic;
using System.Linq;

namespace DomainTacticalPatternCompliance.Main.StructureInfo
{
    /// <summary>Jenis edge ClusteringGraph (Iliopoulos 2025, 4.2.2).</summary>
    public enum EdgeType
    {
        Reference,   // 0.1, disesuaikan ke [0,1] di Fase 3
        Coupled,     // 1.5
        Ownership,   // 2.0
        Embedded,    // 2.0
        Value        // 2.0
    }

    public static class EdgeTypeInfo
    {
        public static double Baseline(this EdgeType t) => t switch
        {
            EdgeType.Reference => 0.1,
            EdgeType.Coupled => 1.5,
            _ => 2.0
        };

        /// <summary>Edge kuat: dipakai SAC untuk menyatukan node.</summary>
        public static bool IsStrong(this EdgeType t) =>
            t is EdgeType.Ownership or EdgeType.Embedded or EdgeType.Value;

        public static string ToText(this EdgeType t) => t.ToString().ToUpperInvariant();
    }

    public sealed class ClusterEdge
    {
        /// <summary>Ujung edge, selalu terurut (A &lt; B) karena graf tak berarah.</summary>
        public string A { get; }
        public string B { get; }
        public EdgeType Type { get; set; }
        public double Weight { get; set; }
        /// <summary>Untuk OWNERSHIP: entity pembuat (arah creator → created). Null untuk jenis lain.</summary>
        public string? Owner { get; set; }
        public string Reason { get; set; }

        public ClusterEdge(string a, string b, EdgeType type, double weight, string reason, string? owner = null)
        {
            (A, B) = string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
            Type = type; Weight = weight; Reason = reason; Owner = owner;
        }

        public string Other(string v) => v == A ? B : A;
        public ClusterEdge Clone() => new(A, B, Type, Weight, Reason, Owner);
    }

    /// <summary>
    /// Graf berbobot tak berarah, satu edge per pasangan node (adjacency lewat dictionary pasangan).
    /// Node = entity, value object, dan enum.
    /// </summary>
    public class ClusteringGraph
    {
        private readonly Dictionary<(string, string), ClusterEdge> _edges = new();
        public SortedSet<string> Nodes { get; } = new(StringComparer.Ordinal);
        public IEnumerable<ClusterEdge> Edges => _edges.Values;

        private static (string, string) Key(string a, string b) =>
            string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);

        public void AddNode(string n) => Nodes.Add(n);

        public ClusterEdge? GetEdge(string a, string b) =>
            _edges.TryGetValue(Key(a, b), out var e) ? e : null;

        /// <summary>Tambah atau timpa edge a–b.</summary>
        public ClusterEdge SetEdge(string a, string b, EdgeType type, string reason,
                                   double? weight = null, string? owner = null)
        {
            Nodes.Add(a); Nodes.Add(b);
            var e = new ClusterEdge(a, b, type, weight ?? type.Baseline(), reason, owner);
            _edges[Key(a, b)] = e;
            return e;
        }

        public IEnumerable<ClusterEdge> EdgesOf(string v) => _edges.Values.Where(e => e.A == v || e.B == v);

        public ClusteringGraph Clone()
        {
            var g = new ClusteringGraph();
            foreach (var n in Nodes) g.AddNode(n);
            foreach (var e in _edges.Values) g._edges[(e.A, e.B)] = e.Clone();
            return g;
        }
    }
}
