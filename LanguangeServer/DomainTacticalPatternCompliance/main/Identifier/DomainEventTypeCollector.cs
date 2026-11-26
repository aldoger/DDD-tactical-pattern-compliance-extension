#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainTacticalPatternCompliance.main.Identifier
{
    /// <summary>
    /// Bukti bahwa sebuah tipe adalah domain event. Sebagian besar bukti diambil dari
    /// tempat event itu DIPAKAI (publisher, dispatcher, handler), bukan dari member kelas event itu sendiri,
    /// karena kelas event biasanya hanya kelas berisi properti (Millett &amp; Tune, Listing 18-1).
    /// </summary>
    [Flags]
    public enum DomainEventEvidence
    {
        None = 0,
        Raised = 1 << 0,           // DomainEvents.Raise(new X(..)), dispatcher.Raise(..), Bus.InMemory.Raise(..), Bus.Publish(..), AddDomainEvent(..)
        Subscribed = 1 << 1,       // DomainEvents.Register<X>(callback) (Udi Dahan), bus.Subscribe<X>()
        NativeEvent = 1 << 2,      // C# `event`: parameter tipe delegate-nya, EventHandler<X>, Action<X>
        Handled = 1 << 3,          // IDomainEventHandler<X>, INotificationHandler<X>, IHandles<X>, IEventHandler<X>
        HandledAsMessage = 1 << 4, // NServiceBus IHandleMessages<X>: bisa command ATAU event -> lemah
        MarkerType = 1 << 5,       // implements IDomainEvent / IEvent / INotification, atau turunan DomainEvent
        EventsNamespace = 1 << 6,  // konvensi NServiceBus: namespace mengandung "Events" (Listing 18-7) -> lemah
        Attribute = 1 << 7,        // [DomainEvent]
    }

    public static class DomainEventTypeCollector
    {
        /// <summary>Bukti yang cukup kuat untuk langsung mengklasifikasikan tipe sebagai DomainEvent.</summary>
        public const DomainEventEvidence Strong =
            DomainEventEvidence.Raised | DomainEventEvidence.Subscribed | DomainEventEvidence.NativeEvent |
            DomainEventEvidence.Handled | DomainEventEvidence.MarkerType | DomainEventEvidence.Attribute;

        // Send(...) sengaja TIDAK dimasukkan: bus.Send mengirim command (mis. RefundDueToLateDelivery), bukan event.
        private static readonly HashSet<string> PublishVerbs = new(StringComparer.Ordinal)
        {
            "Raise", "Publish", "Dispatch", "AddDomainEvent", "RaiseDomainEvent", "AddEvent"
        };

        private static readonly HashSet<string> SubscribeVerbs = new(StringComparer.Ordinal)
        {
            "Register", "Subscribe"
        };

        private static readonly HashSet<string> EventHandlerInterfaces = new(StringComparer.Ordinal)
        {
            "IDomainEventHandler", "IEventHandler", "INotificationHandler", "IHandles", "IHandle"
        };

        private static readonly HashSet<string> MessageHandlerInterfaces = new(StringComparer.Ordinal)
        {
            "IHandleMessages"
        };

        private static readonly HashSet<string> MarkerTypes = new(StringComparer.Ordinal)
        {
            "IDomainEvent", "IEvent", "INotification", "DomainEvent", "DomainEventBase"
        };

        /// <summary>
        /// Pass pertama: telusuri SELURUH proyek dan kumpulkan tipe yang di-raise, di-subscribe,
        /// di-handle, atau ditandai sebagai event. Hasilnya dipakai saat mengklasifikasikan tiap tipe.
        /// </summary>
        public static IReadOnlyDictionary<string, DomainEventEvidence> Collect(IEnumerable<SyntaxTree> trees)
        {
            var roots = trees.Select(t => t.GetRoot()).ToList();

            var declaredTypes = roots
                .SelectMany(r => r.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                .Select(t => t.Identifier.Text)
                .ToHashSet(StringComparer.Ordinal);

            var delegates = roots
                .SelectMany(r => r.DescendantNodes().OfType<DelegateDeclarationSyntax>())
                .GroupBy(d => d.Identifier.Text)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            var evidence = new Dictionary<string, DomainEventEvidence>(StringComparer.Ordinal);
            void Add(string? name, DomainEventEvidence e)
            {
                if (string.IsNullOrEmpty(name) || !declaredTypes.Contains(name)) return; // abaikan tipe framework
                evidence[name] = evidence.TryGetValue(name, out var cur) ? cur | e : e;
            }

            foreach (var root in roots)
            {
                // 1) Raise/Publish/Register/Subscribe di call site
                foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var (verb, typeArgs, receiver) = Describe(inv.Expression);
                    if (verb is null) continue;
                    if (verb.EndsWith("Async", StringComparison.Ordinal)) verb = verb[..^"Async".Length];

                    if (PublishVerbs.Contains(verb))
                    {
                        foreach (var t in typeArgs) Add(SimpleName(t), DomainEventEvidence.Raised);
                        foreach (var a in inv.ArgumentList.Arguments)
                            Add(CreatedTypeName(a.Expression, inv), DomainEventEvidence.Raised);
                    }
                    else if (SubscribeVerbs.Contains(verb) && typeArgs.Count == 1 && LooksLikeEventInfrastructure(receiver))
                    {
                        // receiver harus "berbau" event/bus agar container.Register<IFoo>() (DI) tidak ikut terhitung
                        Add(SimpleName(typeArgs[0]), DomainEventEvidence.Subscribed);
                    }
                }

                // 2) Native C# event: tipe event diambil dari delegate-nya, BUKAN dari kelas yang mendeklarasikan `event`
                var eventTypes = root.DescendantNodes().OfType<EventFieldDeclarationSyntax>().Select(e => e.Declaration.Type)
                    .Concat(root.DescendantNodes().OfType<EventDeclarationSyntax>().Select(e => e.Type));
                foreach (var t in eventTypes)
                {
                    if (t is GenericNameSyntax g && g.Identifier.Text is "EventHandler" or "Action")
                    {
                        foreach (var arg in g.TypeArgumentList.Arguments) Add(SimpleName(arg), DomainEventEvidence.NativeEvent);
                    }
                    else if (delegates.TryGetValue(SimpleName(t), out var d))
                    {
                        foreach (var p in d.ParameterList.Parameters.Where(p => p.Type != null))
                        {
                            var n = SimpleName(p.Type!);
                            if (n is "object" or "Object") continue; // parameter `sender`
                            Add(n, DomainEventEvidence.NativeEvent);
                        }
                    }
                }

                // 3) Handler & penanda pada deklarasi tipe
                foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    string self = type.Identifier.Text;

                    foreach (var bt in type.BaseList?.Types ?? default)
                    {
                        if (bt.Type is GenericNameSyntax g && g.TypeArgumentList.Arguments.Count >= 1)
                        {
                            var first = SimpleName(g.TypeArgumentList.Arguments[0]);
                            if (EventHandlerInterfaces.Contains(g.Identifier.Text)) Add(first, DomainEventEvidence.Handled);
                            else if (MessageHandlerInterfaces.Contains(g.Identifier.Text)) Add(first, DomainEventEvidence.HandledAsMessage);
                        }
                        else if (MarkerTypes.Contains(SimpleName(bt.Type)))
                        {
                            Add(self, DomainEventEvidence.MarkerType);
                        }
                    }

                    if (type.AttributeLists.SelectMany(l => l.Attributes)
                        .Any(a => SimpleName(a.Name) is "DomainEvent" or "DomainEventAttribute"))
                        Add(self, DomainEventEvidence.Attribute);

                    if (type is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax &&
                        NamespaceOf(type).Contains("Events", StringComparison.Ordinal))
                        Add(self, DomainEventEvidence.EventsNamespace);
                }
            }

            return evidence;
        }

        /// <summary>Bukti untuk satu tipe; <see cref="DomainEventEvidence.None"/> jika tidak ada.</summary>
        public static DomainEventEvidence EvidenceFor(IReadOnlyDictionary<string, DomainEventEvidence>? evidence, string typeName) =>
            evidence != null && evidence.TryGetValue(typeName, out var e) ? e : DomainEventEvidence.None;

        public static bool IsStrong(DomainEventEvidence e) => (e & Strong) != 0;

        // ------------------------------------------------------------ helpers

        private static (string? verb, IReadOnlyList<TypeSyntax> typeArgs, ExpressionSyntax? receiver) Describe(ExpressionSyntax e)
        {
            SimpleNameSyntax? name = e switch
            {
                MemberAccessExpressionSyntax ma => ma.Name,
                MemberBindingExpressionSyntax mb => mb.Name,   // x?.Raise(...)
                SimpleNameSyntax s => s,                          // Raise(...), AddDomainEvent(...) dari base class
                _ => null
            };
            if (name is null) return (null, Array.Empty<TypeSyntax>(), null);

            var typeArgs = name is GenericNameSyntax g ? g.TypeArgumentList.Arguments.ToList() : new List<TypeSyntax>();
            var receiver = (e as MemberAccessExpressionSyntax)?.Expression;
            return (name.Identifier.Text, typeArgs, receiver);
        }

        private static bool LooksLikeEventInfrastructure(ExpressionSyntax? receiver)
        {
            if (receiver is null) return false;
            var text = receiver.ToString();
            return new[] { "Event", "Bus", "Dispatcher", "Publisher", "Mediator" }
                .Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        // `new X(..)` langsung, atau variabel lokal yang diinisialisasi dengan `new X(..)`
        private static string? CreatedTypeName(ExpressionSyntax e, SyntaxNode site)
        {
            while (e is ParenthesizedExpressionSyntax p) e = p.Expression;

            if (e is ObjectCreationExpressionSyntax oc) return SimpleName(oc.Type);

            if (e is IdentifierNameSyntax id)
            {
                var body = site.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>() as SyntaxNode
                           ?? site.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
                var decl = body?.DescendantNodes().OfType<VariableDeclaratorSyntax>()
                    .FirstOrDefault(v => v.Identifier.Text == id.Identifier.Text);
                if (decl?.Initializer?.Value is ObjectCreationExpressionSyntax init) return SimpleName(init.Type);
            }
            return null; // `new(...)` tanpa tipe butuh SemanticModel
        }

        private static string NamespaceOf(SyntaxNode node) =>
            string.Join(".", node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()));

        private static string SimpleName(TypeSyntax t) => t switch
        {
            GenericNameSyntax g => g.Identifier.Text,
            IdentifierNameSyntax i => i.Identifier.Text,
            QualifiedNameSyntax q => SimpleName(q.Right),
            AliasQualifiedNameSyntax aq => SimpleName(aq.Name),
            NullableTypeSyntax nt => SimpleName(nt.ElementType),
            PredefinedTypeSyntax pt => pt.Keyword.Text,
            _ => t.ToString()
        };
    }
}
