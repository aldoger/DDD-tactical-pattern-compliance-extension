using GettingStartedCS.main.StructureInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GettingStartedCS.main.Identifier
{
    public enum MethodStereotype
    {
        Unclassified,

        // Structural
        Constructor,
        CopyConstructor,
        Destructor,
        Empty,

        // Accessors
        Get,
        Predicate,
        Property,
        VoidAccessor,

        // Mutators
        Set,
        Command,
        NonVoidCommand,

        // Creational
        Factory,

        // Collaborational
        Collaborator,
        Controller,
        Wrapper,

        // Degenerate
        Incidental,
        Stateless
    }

    public enum ClassStereotype
    {
        Unclassified,
        Entity,
        MinimalEntity,
        DataProvider,
        Commander,
        Boundary,
        Factory,
        Controller,
        PureController,
        LargeClass,
        LazyClass,
        Degenerate,
        DataClass,
        SmallClass,
        Empty,
    }

    public static class StereotypeExtensions
    {
        // ---------- Method stereotype <-> string ----------
        private static readonly Dictionary<MethodStereotype, string> MethodToText = new()
        {
            [MethodStereotype.Unclassified] = "unclassified",
            [MethodStereotype.Constructor] = "constructor",
            [MethodStereotype.CopyConstructor] = "copy-constructor",
            [MethodStereotype.Destructor] = "destructor",
            [MethodStereotype.Empty] = "empty",
            [MethodStereotype.Get] = "get",
            [MethodStereotype.Predicate] = "predicate",
            [MethodStereotype.Property] = "property",
            [MethodStereotype.VoidAccessor] = "void-accessor",
            [MethodStereotype.Set] = "set",
            [MethodStereotype.Command] = "command",
            [MethodStereotype.NonVoidCommand] = "non-void-command",
            [MethodStereotype.Factory] = "factory",
            [MethodStereotype.Collaborator] = "collaborator",
            [MethodStereotype.Controller] = "controller",
            [MethodStereotype.Wrapper] = "wrapper",
            [MethodStereotype.Incidental] = "incidental",
            [MethodStereotype.Stateless] = "stateless",
        };

        private static readonly Dictionary<string, MethodStereotype> TextToMethod =
            MethodToText.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

        public static string ToText(this MethodStereotype s) => MethodToText[s];

        public static bool TryParseMethodStereotype(string text, out MethodStereotype result)
            => TextToMethod.TryGetValue(text.Trim(), out result);

        // ---------- Class stereotype <-> string ----------

        private static readonly Dictionary<ClassStereotype, string> ClassToText = new()
        {
            [ClassStereotype.Unclassified] = "unclassified",
            [ClassStereotype.Entity] = "entity",
            [ClassStereotype.MinimalEntity] = "minimal-entity",
            [ClassStereotype.DataProvider] = "data-provider",
            [ClassStereotype.Commander] = "commander",
            [ClassStereotype.Boundary] = "boundary",
            [ClassStereotype.Factory] = "factory",
            [ClassStereotype.Controller] = "controller",
            [ClassStereotype.PureController] = "pure-controller",
            [ClassStereotype.LargeClass] = "large-class",
            [ClassStereotype.LazyClass] = "lazy-class",
            [ClassStereotype.Degenerate] = "degenerate",
            [ClassStereotype.DataClass] = "data-class",
            [ClassStereotype.SmallClass] = "small-class",
            [ClassStereotype.Empty] = "empty",
        };

        private static readonly Dictionary<string, ClassStereotype> TextToClass =
            ClassToText.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

        public static string ToText(this ClassStereotype s) => ClassToText[s];

        public static bool TryParseClassStereotype(string text, out ClassStereotype result)
            => TextToClass.TryGetValue(text.Trim(), out result);

        // ---------- Groups used by the class-level rules ----------

        public static bool IsAccessor(this MethodStereotype s) =>
            s is MethodStereotype.Get or MethodStereotype.Predicate
              or MethodStereotype.Property or MethodStereotype.VoidAccessor;

        public static bool IsMutator(this MethodStereotype s) =>
            s is MethodStereotype.Set or MethodStereotype.Command
              or MethodStereotype.NonVoidCommand;

        public static bool IsCollaborator(this MethodStereotype s) =>
            s is MethodStereotype.Collaborator or MethodStereotype.Controller
              or MethodStereotype.Wrapper;

        public static bool IsDegenerate(this MethodStereotype s) =>
            s is MethodStereotype.Incidental or MethodStereotype.Stateless
              or MethodStereotype.Empty;
    }

    public sealed record ClassThresholds
    {
        // indicates for large class
        public int MethodsPerClassThreshold { get; init; } = 40;
    }

    public readonly record struct MethodCounts(
        int Getters, int Accessors,
        int Setters, int Commands,
        int Controllers, int Collaborators,
        int Factories, int NonCollaborators,
        int Degenerates, int AllMethods)
    {
        public int Mutators => Setters + Commands;
        public int NonGetAccessors => Accessors - Getters;
        public int NonSetMutators => Mutators - Setters;

        // collaborator + wrapper only (Collaborators here already includes controllers)
        public int CollaboratorsOnly => Collaborators - Controllers;
    }

    public class ClassStereotypeIdentifier
    {
        private readonly ClassThresholds _thresholds;

        public ClassStereotypeIdentifier() : this(new ClassThresholds()) { }

        public ClassStereotypeIdentifier(ClassThresholds thresholds)
        {
            _thresholds = thresholds;
        }

        public void IdentifyStereotype(ClassStructureInfo classStructure)
        {
            MethodCounts c = CountMethods(classStructure);

            // Keep the summary fields on the class info in sync
            classStructure.TotalAccesor = c.Accessors;
            classStructure.TotalMutator = c.Mutators;
            classStructure.TotalCollaborator = c.Collaborators;
            classStructure.HasFactoryMethods = c.Factories > 0;

            if (IsEntity(c)) AddOnce(classStructure, ClassStereotype.Entity);
            if (IsMinimalEntity(c)) AddOnce(classStructure, ClassStereotype.MinimalEntity);
            if (IsDataProvider(c)) AddOnce(classStructure, ClassStereotype.DataProvider);
            if (IsCommander(c)) AddOnce(classStructure, ClassStereotype.Commander);
            if (IsBoundary(c)) AddOnce(classStructure, ClassStereotype.Boundary);
            if (IsFactory(c)) AddOnce(classStructure, ClassStereotype.Factory);
            if (IsController(c)) AddOnce(classStructure, ClassStereotype.Controller);
            if (IsPureController(c)) AddOnce(classStructure, ClassStereotype.PureController);
            if (IsLargeClass(c)) AddOnce(classStructure, ClassStereotype.LargeClass);
            if (IsLazyClass(c)) AddOnce(classStructure, ClassStereotype.LazyClass);
            if (IsDegenerate(c)) AddOnce(classStructure, ClassStereotype.Degenerate);
            if (IsDataClass(c)) AddOnce(classStructure, ClassStereotype.DataClass);
            if (IsSmallClass(c)) AddOnce(classStructure, ClassStereotype.SmallClass);
            if (IsEmpty(c)) AddOnce(classStructure, ClassStereotype.Empty);

            if (classStructure.Stereotype.Count == 0)
                AddOnce(classStructure, ClassStereotype.Unclassified);
        }

        // ---------------- class rules ----------------

        private bool IsEntity(MethodCounts c)
        {
            if (c.NonGetAccessors == 0 || c.NonSetMutators == 0)
                return false;

            if (c.Controllers > 0)
                return false;

            return true;
        }

        private bool IsMinimalEntity(MethodCounts c)
        {
            if (c.AllMethods - (c.Getters + c.Setters + c.Commands) != 0) return false;
            if (c.Getters == 0 || c.Setters == 0 || c.Commands == 0) return false;

            return true;
        }

        private bool IsDataProvider(MethodCounts c)
            => c.Accessors > 0 && c.Factories > 0;

        private bool IsCommander(MethodCounts c)
            => (c.Mutators > 2 * c.Accessors) && (c.Mutators > 2 * (c.Controllers + c.Factories));

        private bool IsBoundary(MethodCounts c)
            => (c.Collaborators > c.NonCollaborators)
            && (c.Factories < 0.5 * c.AllMethods)
            && (c.Controllers < 0.33 * c.AllMethods);

        private bool IsFactory(MethodCounts c)
            => c.Factories > 0.67 * c.AllMethods;

        private bool IsController(MethodCounts c)
            => (c.Controllers + c.Factories > 0.67 * c.AllMethods)
            && (c.Accessors != 0 || c.Mutators != 0);

        private bool IsPureController(MethodCounts c)
            => (c.Controllers + c.Factories != 0)
            && (c.Accessors + c.Mutators + c.CollaboratorsOnly == 0)
            && (c.Controllers != 0);

        private bool IsLargeClass(MethodCounts c)
        {
            int accPlusMut = c.Accessors + c.Mutators;
            int facPlusCon = c.Controllers + c.Factories;

            bool balanced =
                (0.2 * c.AllMethods < accPlusMut) && (accPlusMut < 0.67 * c.AllMethods) &&
                (0.2 * c.AllMethods < facPlusCon) && (facPlusCon < 0.67 * c.AllMethods) &&
                c.Factories != 0 && c.Controllers != 0 && c.Accessors != 0 && c.Mutators != 0;

            return balanced && c.AllMethods > _thresholds.MethodsPerClassThreshold;
        }

        private bool IsLazyClass(MethodCounts c)
        {
            if (c.AllMethods == 0) return false;

            double degenerateRatio = c.Degenerates / (double)c.AllMethods;
            double otherRatio = (c.AllMethods - (c.Degenerates + c.Getters + c.Setters)) / (double)c.AllMethods;

            return (c.Getters + c.Setters != 0) && degenerateRatio > 0.33 && otherRatio <= 0.2;
        }

        private bool IsDegenerate(MethodCounts c)
            => c.AllMethods > 0 && (c.Degenerates / (double)c.AllMethods) > 0.5;

        private bool IsDataClass(MethodCounts c)
            => (c.AllMethods - (c.Getters + c.Setters) == 0) && (c.Getters + c.Setters != 0);

        private bool IsSmallClass(MethodCounts c)
            => c.AllMethods > 0 && c.AllMethods < 3;

        private bool IsEmpty(MethodCounts c)
            => c.AllMethods == 0;

        // ---------------- counting ----------------

        private static MethodCounts CountMethods(ClassStructureInfo cls)
        {
            int getters = 0, accessors = 0, setters = 0, commands = 0;
            int controllers = 0, collaborators = 0, factories = 0, nonCollaborators = 0;
            int degenerates = 0, allMethods = 0;

            foreach (MethodStructureInfo method in cls.Methods)
            {
                var stereotypes = method.Stereotype;

                // Constructors / destructors are not part of class stereotype computation
                if (stereotypes.Any(s => s is MethodStereotype.Constructor
                                           or MethodStereotype.CopyConstructor
                                           or MethodStereotype.Destructor)) continue;

                allMethods++;   // = methods.Count - constructor/destructor count

                foreach (MethodStereotype s in stereotypes)
                {
                    if (s == MethodStereotype.Get) getters++;
                    if (s.IsAccessor()) accessors++;            // get + predicate + property + void-accessor
                    if (s == MethodStereotype.Set) setters++;
                    if (s is MethodStereotype.Command or MethodStereotype.NonVoidCommand) commands++;
                    if (s == MethodStereotype.Controller) controllers++;
                    if (s.IsCollaborator()) collaborators++;    // controller + collaborator + wrapper
                    if (s == MethodStereotype.Factory) factories++;
                    if (s.IsDegenerate()) degenerates++;        // incidental + stateless + empty
                }

                if (!stereotypes.Any(s => s.IsCollaborator()))
                    nonCollaborators++;
            }

            return new MethodCounts(getters, accessors, setters, commands,
                                    controllers, collaborators, factories, nonCollaborators,
                                    degenerates, allMethods);
        }

        private static void AddOnce(ClassStructureInfo cls, ClassStereotype s)
        {
            if (!cls.Stereotype.Contains(s))
                cls.AddStereotype(s);
        }
    }

    public static class MethodStereotypeClassifier
    {
        public static List<MethodStereotype> Classify(MethodFacts f)
        {
            var result = new List<MethodStereotype>();

            // empty: no statements except comments
            if (f.NonCommentStatements == 0)
            {
                result.Add(MethodStereotype.Empty);
                return result;
            }

            string rt = f.ReturnType;
            bool isVoid = (rt == "void" || rt == "Void") && !f.IsVoidPointer;
            bool isBool = rt is "bool" or "Boolean" or "System.Boolean";
            bool returnNotVoidOrBool = (!isBool && rt != "void" && rt != "Void" && rt != "") || f.IsVoidPointer;
            bool usesState = f.DataMemberUsed || f.CallsOnClassMethods > 0;

            // ---------------- accessors ----------------
            if (f.SimpleReturn)
                result.Add(MethodStereotype.Get);

            if (isBool && f.ComplexReturn && usesState)
                result.Add(MethodStereotype.Predicate);

            if (returnNotVoidOrBool && f.ComplexReturn && usesState)
                result.Add(MethodStereotype.Property);

            if (isVoid && f.ParameterRefModified && usesState)
                result.Add(MethodStereotype.VoidAccessor);

            // ---------------- mutators ----------------
            if (f.CallsOnClassMethods == 0 &&
                ((f.DataMembersModified == 1 && f.CallsOnDataMembers == 0) ||
                 (f.DataMembersModified == 0 && f.CallsOnDataMembers == 1)))
                result.Add(MethodStereotype.Set);

            bool case1 = f.DataMembersModified > 1;
            bool case2 = f.DataMembersModified == 1 && (f.CallsOnClassMethods + f.CallsOnDataMembers) > 0;
            bool case3 = f.DataMembersModified == 0 && (f.CallsOnDataMembers > 1 || f.CallsOnClassMethods > 0);
            if (case1 || case2 || case3)   // no 'const' methods in C#, so no mutable-member special case
                result.Add(isVoid ? MethodStereotype.Command : MethodStereotype.NonVoidCommand);

            // ---------------- factory ----------------
            if (f.NonPrimitiveReturnType && (f.NewReturned || f.VariableCreatedAndReturnedWithNew))
                result.Add(MethodStereotype.Factory);

            // ---------------- wrapper / controller / collaborator ----------------
            bool noOwnState = f.DataMembersModified == 0 && f.CallsOnClassMethods == 0 && f.CallsOnDataMembers == 0;

            if (noOwnState && f.ExternalMethodCalls == 0 && f.ExternalFunctionCalls > 0)
                result.Add(MethodStereotype.Wrapper);
            else if (noOwnState && (f.ExternalMethodCalls > 0 || f.NonPrimitiveLocalOrParameterModified))
                result.Add(MethodStereotype.Controller);
            else if (f.NonPrimitiveDataMemberExternal || f.NonPrimitiveLocalExternal ||
                     f.NonPrimitiveParameterExternal || f.NonPrimitiveReturnTypeExternal || f.IsVoidPointer)
                result.Add(MethodStereotype.Collaborator);

            // ---------------- degenerate ----------------
            bool noCalls = f.CallsOnClassMethods == 0 && f.CallsOnDataMembers == 0 &&
                           f.NewConstructorCalls == 0 && f.ExternalMethodCalls == 0 && f.ExternalFunctionCalls == 0;

            if (!f.DataMemberUsed && noCalls)
                result.Add(MethodStereotype.Incidental);

            if (!f.DataMemberUsed && f.CallsOnClassMethods == 0 && f.CallsOnDataMembers == 0 &&
                (f.ExternalFunctionCalls > 0 || f.ExternalMethodCalls > 0 || f.NewConstructorCalls > 0))
                result.Add(MethodStereotype.Stateless);

            if (result.Count == 0)
                result.Add(MethodStereotype.Unclassified);

            return result;
        }
    }
}