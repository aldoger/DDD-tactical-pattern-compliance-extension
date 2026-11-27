#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DomainTacticalPatternCompliance.main.Identifier
{
    [Flags]
    public enum DomainEventEvidence
    {
        None = 0,
        Raised = 1 << 0,           // DomainEvents.Raise(new X(..)), dispatcher.Raise(..), Bus.InMemory.Raise(..), Bus.Publish(..), AddDomainEvent(..)
        Subscribed = 1 << 1,       // DomainEvents.Register<X>(..) (Udi Dahan), bus.Subscribe<X>()
        NativeEvent = 1 << 2,      // C# `event`: the delegate's parameter type, EventHandler<X>, Action<X>
        Handled = 1 << 3,          // IDomainEventHandler<X>, INotificationHandler<X>, IHandles<X>, IEventHandler<X>
        HandledAsMessage = 1 << 4, // NServiceBus IHandleMessages<X>: command OR event -> weak
        MarkerType = 1 << 5,       // implements IDomainEvent / IEvent / INotification, or derives from DomainEvent
        EventsNamespace = 1 << 6,  // NServiceBus convention: namespace contains "Events" (Listing 18-7) -> weak
        Attribute = 1 << 7,        // [DomainEvent]
        EventName = 1 << 8,        // class name ends with "Event" (e.g. OrderPlacedEvent) -> weak
    }

    /// <summary>
    /// One class taking part in a domain event: a publisher or a subscriber.
    /// <paramref name="Mechanism"/> says HOW it was recognised, <paramref name="Location"/> is file:line.
    /// </summary>
    public sealed record EventParticipant(string TypeName, string Mechanism, string? Member, string Location);

    public sealed class DomainEventInfo
    {
        public DomainEventInfo(string eventType) => EventType = eventType;

        public string EventType { get; }
        public DomainEventEvidence Evidence { get; internal set; }
        public bool IsStrong => DomainEventTypeCollector.IsStrong(Evidence);

        public List<EventParticipant> Publishers { get; } = new();
        public List<EventParticipant> Subscribers { get; } = new();
    }

    public static class DomainEventTypeCollector
    {
        /// <summary>Evidence strong enough to classify a type as a DomainEvent on its own.</summary>
        public const DomainEventEvidence Strong =
            DomainEventEvidence.Raised | DomainEventEvidence.Subscribed | DomainEventEvidence.NativeEvent |
            DomainEventEvidence.Handled | DomainEventEvidence.MarkerType | DomainEventEvidence.Attribute;

        // Send(...) is deliberately NOT here: bus.Send sends a command (e.g. RefundDueToLateDelivery), not an event.
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

        // ------------------------------------------------------------ helpers shared with the identifier

        /// <summary>True when the evidence alone is enough to classify the type as a DomainEvent.</summary>
        public static bool IsStrong(DomainEventEvidence evidence) => (evidence & Strong) != 0;

        /// <summary>Naming rule shared with DomainModelSignalExtractor.HasEventName.</summary>
        public static bool HasEventName(string typeName) =>
            typeName.EndsWith("Event", StringComparison.Ordinal) && typeName != "Event";

        /// <summary>Evidence collected for one type, or None when the type never appeared as an event.</summary>
        public static DomainEventEvidence EvidenceFor(IReadOnlyDictionary<string, DomainEventInfo>? domainEvents, string typeName) =>
            InfoFor(domainEvents, typeName)?.Evidence ?? DomainEventEvidence.None;

        /// <summary>Everything collected for one type (evidence, publishers, subscribers), or null.</summary>
        public static DomainEventInfo? InfoFor(IReadOnlyDictionary<string, DomainEventInfo>? domainEvents, string typeName) =>
            domainEvents != null && domainEvents.TryGetValue(typeName, out var info) ? info : null;

        // ------------------------------------------------------------ collection

        /// <summary>
        /// Pass 1 (evidence only): which types are domain events, and why.
        /// </summary>
        public static IReadOnlyDictionary<string, DomainEventEvidence> Collect(IEnumerable<SyntaxTree> trees) =>
            CollectDetailed(trees).ToDictionary(kv => kv.Key, kv => kv.Value.Evidence, StringComparer.Ordinal);

        /// <summary>
        /// Pass 1 with relations: every domain event plus its publishers and subscribers (for C9).
        /// </summary>
        public static IReadOnlyDictionary<string, DomainEventInfo> CollectDetailed(IEnumerable<SyntaxTree> trees)
        {
            var treeList = trees.ToList();
            var roots = treeList.Select(t => t.GetRoot()).ToList();

            var declaredTypes = roots
                .SelectMany(r => r.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                .Select(t => t.Identifier.Text)
                .ToHashSet(StringComparer.Ordinal);

            var delegates = roots
                .SelectMany(r => r.DescendantNodes().OfType<DelegateDeclarationSyntax>())
                .GroupBy(d => d.Identifier.Text)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            var typeDecls = roots.SelectMany(r => r.DescendantNodes().OfType<TypeDeclarationSyntax>()).ToList();
            var markerNames = MarkerNamesIncludingProjectBases(typeDecls);

            var infos = new Dictionary<string, DomainEventInfo>(StringComparer.Ordinal);
            DomainEventInfo? Info(string? name)
            {
                if (string.IsNullOrEmpty(name) || !declaredTypes.Contains(name)) return null; // ignore framework types
                if (!infos.TryGetValue(name, out var info)) infos[name] = info = new DomainEventInfo(name);
                return info;
            }
            void AddEvidence(string? name, DomainEventEvidence e)
            {
                var info = Info(name);
                if (info != null) info.Evidence |= e;
            }

            // Creation sites are only meaningful once we know which types are events, so keep them for later.
            var creations = new List<(string eventType, string mechanism, SyntaxNode node)>();
            var publishers = new List<(string eventType, string mechanism, SyntaxNode node, string? typeName)>();
            var subscribers = new List<(string eventType, string mechanism, SyntaxNode node, string? typeName)>();

            // C# `event` members: member name -> event types carried by its delegate
            var eventMembers = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var root in roots)
            {
                // ---- C# `event` keyword (Listing 18-4): the DECLARING type is the publisher,
                //      the delegate's parameter type is the event
                foreach (var (typeSyntax, names, node) in EventMemberDeclarations(root))
                {
                    var carried = EventTypesCarriedBy(typeSyntax, delegates).ToList();
                    foreach (var ev in carried)
                    {
                        AddEvidence(ev, DomainEventEvidence.NativeEvent);
                        foreach (var n in names)
                        {
                            publishers.Add((ev, $"declares C# event '{n}'", node, null));
                            if (!eventMembers.TryGetValue(n, out var list)) eventMembers[n] = list = new List<string>();
                            list.Add(ev);
                        }
                    }
                }
            }

            foreach (var root in roots)
            {
                // ---- Invocations: Raise/Publish (publisher) and Register/Subscribe (subscriber)
                foreach (var inv in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var (verb, typeArgs, receiver) = Describe(inv.Expression);
                    if (verb is null) continue;
                    if (verb.EndsWith("Async", StringComparison.Ordinal)) verb = verb[..^"Async".Length];

                    if (PublishVerbs.Contains(verb))
                    {
                        string how = $"{(receiver != null ? receiver + "." : "")}{verb}(...)";
                        foreach (var t in typeArgs)
                        {
                            AddEvidence(SimpleName(t), DomainEventEvidence.Raised);
                            publishers.Add((SimpleName(t), $"calls {how}", inv, null));
                        }
                        foreach (var a in inv.ArgumentList.Arguments)
                        {
                            var created = CreatedTypeName(a.Expression, inv);
                            AddEvidence(created, DomainEventEvidence.Raised);
                            if (created != null) publishers.Add((created, $"calls {how}", inv, null));
                        }
                    }
                    else if (SubscribeVerbs.Contains(verb) && typeArgs.Count == 1 && LooksLikeEventInfrastructure(receiver))
                    {
                        // receiver must look like event infrastructure so that container.Register<IFoo>() (DI) is ignored
                        var ev = SimpleName(typeArgs[0]);
                        AddEvidence(ev, DomainEventEvidence.Subscribed);

                        // Register<X>(callback): the enclosing class is the subscriber.
                        // Subscribe<X>(new Handler()): the handler class is the subscriber (found below via its interface).
                        bool passesHandlerInstance = inv.ArgumentList.Arguments.Any(a => a.Expression is ObjectCreationExpressionSyntax);
                        if (!passesHandlerInstance)
                            subscribers.Add((ev, $"registers callback via {receiver}.{verb}<{ev}>()", inv, null));
                    }
                    else if (eventMembers.TryGetValue(verb, out var evs) && receiver is null or ThisExpressionSyntax)
                    {
                        // Invoking a C# event from inside its own class: DeliveryGuaranteeFailed(new DeliveryGuaranteeFailed(this))
                        foreach (var a in inv.ArgumentList.Arguments)
                        {
                            var created = CreatedTypeName(a.Expression, inv);
                            if (created != null && evs.Contains(created))
                                publishers.Add((created, $"invokes C# event '{verb}'", inv, null));
                        }
                    }
                }

                // ---- `x.SomeEvent += handler` (Listing 18-5): the enclosing class subscribes to a C# event
                foreach (var asg in root.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                             .Where(a => a.IsKind(SyntaxKind.AddAssignmentExpression)))
                {
                    string? member = asg.Left switch
                    {
                        MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
                        IdentifierNameSyntax id => id.Identifier.Text,
                        _ => null
                    };
                    if (member != null && eventMembers.TryGetValue(member, out var evs))
                        foreach (var ev in evs)
                            subscribers.Add((ev, $"subscribes with '{asg.Left} +='", asg, null));
                }

                // ---- Every `new X(...)`: becomes a publisher if X turns out to be an event
                foreach (var oc in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                    creations.Add((SimpleName(oc.Type), "creates the event", oc));

                // ---- Type declarations: handler interfaces (subscribers), markers, attributes, namespace
                foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    string self = type.Identifier.Text;

                    foreach (var bt in type.BaseList?.Types ?? default)
                    {
                        if (bt.Type is GenericNameSyntax g && g.TypeArgumentList.Arguments.Count >= 1)
                        {
                            var first = SimpleName(g.TypeArgumentList.Arguments[0]);
                            if (EventHandlerInterfaces.Contains(g.Identifier.Text))
                            {
                                AddEvidence(first, DomainEventEvidence.Handled);
                                subscribers.Add((first, $"implements {g}", type, self));
                            }
                            else if (MessageHandlerInterfaces.Contains(g.Identifier.Text))
                            {
                                AddEvidence(first, DomainEventEvidence.HandledAsMessage);
                                subscribers.Add((first, $"implements {g}", type, self));
                            }
                        }
                    }

                    // Interfaces, abstract base classes and static classes are never events themselves
                    // (IDomainEvent, abstract DomainEventBase, static DomainEvents).
                    if (!IsConcrete(type)) continue;

                    if (BaseNames(type).Any(markerNames.Contains))
                        AddEvidence(self, DomainEventEvidence.MarkerType);

                    if (type.AttributeLists.SelectMany(l => l.Attributes)
                        .Any(a => SimpleName(a.Name) is "DomainEvent" or "DomainEventAttribute"))
                        AddEvidence(self, DomainEventEvidence.Attribute);

                    if (NamespaceOf(type).Contains("Events", StringComparison.Ordinal))
                        AddEvidence(self, DomainEventEvidence.EventsNamespace);

                    if (HasEventName(self))
                        AddEvidence(self, DomainEventEvidence.EventName);
                }
            }

            // ---- Attach relations, now that we know which types are events
            foreach (var (ev, how, node) in creations)
            {
                if (!infos.ContainsKey(ev)) continue;
                // A creation that is already recorded with a more specific mechanism adds nothing.
                bool alreadyCovered = publishers.Any(p => p.eventType == ev && p.node.Span.Contains(node.Span));
                if (!alreadyCovered) publishers.Add((ev, how, node, null));
            }

            foreach (var (ev, how, node, typeName) in publishers)
                AddParticipant(infos, ev, how, node, typeName, isPublisher: true);
            foreach (var (ev, how, node, typeName) in subscribers)
                AddParticipant(infos, ev, how, node, typeName, isPublisher: false);

            return infos;
        }

        // ------------------------------------------------------------ relations

        private static void AddParticipant(Dictionary<string, DomainEventInfo> infos, string ev, string how,
                                           SyntaxNode node, string? typeName, bool isPublisher)
        {
            if (!infos.TryGetValue(ev, out var info)) return;

            typeName ??= node.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>()?.Identifier.Text ?? "<top-level>";
            if (typeName == ev) return; // the event class creating itself (factory/With) is not a publisher

            var p = new EventParticipant(typeName, how, MemberName(node), Location(node));
            var list = isPublisher ? info.Publishers : info.Subscribers;
            if (!list.Any(x => x.TypeName == p.TypeName && x.Mechanism == p.Mechanism && x.Member == p.Member))
                list.Add(p);
        }

        private static string? MemberName(SyntaxNode node) => node.FirstAncestorOrSelf<MemberDeclarationSyntax>() switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text + "()",
            ConstructorDeclarationSyntax c => c.Identifier.Text + "() ctor",
            PropertyDeclarationSyntax p => p.Identifier.Text,
            EventFieldDeclarationSyntax e => string.Join(", ", e.Declaration.Variables.Select(v => v.Identifier.Text)),
            EventDeclarationSyntax e => e.Identifier.Text,
            _ => null
        };

        private static string Location(SyntaxNode node)
        {
            var span = node.GetLocation().GetLineSpan();
            var file = string.IsNullOrEmpty(span.Path) ? "" : Path.GetFileName(span.Path) + ":";
            return $"{file}{span.StartLinePosition.Line + 1}";
        }

        private static IEnumerable<(TypeSyntax type, IReadOnlyList<string> names, SyntaxNode node)> EventMemberDeclarations(SyntaxNode root)
        {
            foreach (var f in root.DescendantNodes().OfType<EventFieldDeclarationSyntax>())
                yield return (f.Declaration.Type, f.Declaration.Variables.Select(v => v.Identifier.Text).ToList(), f);
            foreach (var e in root.DescendantNodes().OfType<EventDeclarationSyntax>())
                yield return (e.Type, new[] { e.Identifier.Text }, e);
        }

        private static IEnumerable<string> EventTypesCarriedBy(TypeSyntax t, IReadOnlyDictionary<string, DelegateDeclarationSyntax> delegates)
        {
            if (t is GenericNameSyntax g && g.Identifier.Text is "EventHandler" or "Action")
            {
                foreach (var arg in g.TypeArgumentList.Arguments) yield return SimpleName(arg);
            }
            else if (delegates.TryGetValue(SimpleName(t), out var d))
            {
                foreach (var p in d.ParameterList.Parameters.Where(p => p.Type != null))
                {
                    var n = SimpleName(p.Type!);
                    if (n is "object" or "Object") continue; // the `sender` parameter
                    yield return n;
                }
            }
        }

        // ------------------------------------------------------------ helpers

        private static (string? verb, IReadOnlyList<TypeSyntax> typeArgs, ExpressionSyntax? receiver) Describe(ExpressionSyntax e)
        {
            SimpleNameSyntax? name = e switch
            {
                MemberAccessExpressionSyntax ma => ma.Name,
                MemberBindingExpressionSyntax mb => mb.Name,   // x?.Raise(...)
                SimpleNameSyntax s => s,                          // Raise(...), AddDomainEvent(...) from a base class
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

        // `new X(..)` directly, or a local variable initialised with `new X(..)`
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
            return null; // `new(...)` without a type needs a SemanticModel
        }

        private static bool IsConcrete(TypeDeclarationSyntax t) =>
            t is not InterfaceDeclarationSyntax &&
            !t.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword) || m.IsKind(SyntaxKind.StaticKeyword));

        private static IEnumerable<string> BaseNames(TypeDeclarationSyntax t) =>
            t.BaseList?.Types.Select(b => SimpleName(b.Type)) ?? Enumerable.Empty<string>();

        // IDomainEvent, DomainEventBase, ... plus every project type that derives from one of them,
        // so `class OrderPlaced : MyEventBase` is found when `abstract class MyEventBase : IDomainEvent`.
        private static HashSet<string> MarkerNamesIncludingProjectBases(IReadOnlyList<TypeDeclarationSyntax> types)
        {
            var markers = new HashSet<string>(MarkerTypes, StringComparer.Ordinal);
            bool changed;
            do
            {
                changed = false;
                foreach (var t in types)
                    if (!markers.Contains(t.Identifier.Text) && BaseNames(t).Any(markers.Contains))
                        changed |= markers.Add(t.Identifier.Text);
            } while (changed);
            return markers;
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