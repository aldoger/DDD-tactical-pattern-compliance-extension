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
        DataProvider,
        Commander,
        Boundary,
        Factory,
        Controller,
        Degenerate,
        PureController,
        LazyClass,
        DataClass,
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
            [ClassStereotype.DataProvider] = "data-provider",
            [ClassStereotype.Commander] = "commander",
            [ClassStereotype.Boundary] = "boundary",
            [ClassStereotype.Factory] = "factory",
            [ClassStereotype.Controller] = "controller",
            [ClassStereotype.PureController] = "pure-controller",
            [ClassStereotype.LazyClass] = "lazy-class",
            [ClassStereotype.Degenerate] = "degenerate",
            [ClassStereotype.DataClass] = "data-class",
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
    
    public sealed record EntityRules
    {
        public bool ForbidControllers { get; init; } = true;

        public double? MinCollaboratorRatio { get; init; } = null;
    }

    public readonly record struct MethodCounts(
       int Getters, int Accessors,
       int Setters, int Commands,
       int Controllers, int Collaborators,
       int Factories, int NonCollaborators)
    {
        public int Mutators => Setters + Commands;
        public int NonGetAccessors => Accessors - Getters;
        public int NonSetMutators => Mutators - Setters; 
    }

    public class ClassStereotypeIdentifier
    {
        private readonly EntityRules _entityRules;
        public ClassStereotypeIdentifier() 
        {
            _entityRules = new EntityRules();
        }
        public void IdentifyStereotype(ClassStructureInfo classStructure)
        {
            MethodCounts counts = CountMethods(classStructure);

            // Keep the summary fields on the class info in sync
            classStructure.TotalAccesor = counts.Accessors;
            classStructure.TotalMutator = counts.Mutators;
            classStructure.TotalCollaborator = counts.Collaborators;
            classStructure.HasFactoryMethods = counts.Factories > 0;

            if (IsEntity(counts))
                AddOnce(classStructure, ClassStereotype.Entity);

            if (classStructure.Stereotype.Count == 0)
                AddOnce(classStructure, ClassStereotype.Unclassified);
        }

        private bool IsEntity(MethodCounts c)
        {
            // Required: accessors AND mutators, beyond plain get/set.
            // (A class with only get/set is a Data Class, not an Entity.)
            if (c.NonGetAccessors == 0 || c.NonSetMutators == 0)
                return false;

            // Optional: controllers disqualify unless explicitly allowed
            if (_entityRules.ForbidControllers && c.Controllers > 0)
                return false;

            // Optional: collaborator ratio
            if (_entityRules.MinCollaboratorRatio is double minRatio)
            {
                double ratio = c.NonCollaborators == 0
                    ? (c.Collaborators > 0 ? double.PositiveInfinity : 0)
                    : (double)c.Collaborators / c.NonCollaborators;

                if (ratio < minRatio)
                    return false;
            }

            // Factory methods never block an Entity, so they are not checked here.
            return true;
        }

        private static MethodCounts CountMethods(ClassStructureInfo cls)
        {
            int getters = 0, accessors = 0, setters = 0, commands = 0;
            int controllers = 0, collaborators = 0, factories = 0, nonCollaborators = 0;

            foreach(MethodStructureInfo method in cls.Methods)
            {
                var stereotypes = method.Stereotype;   

                if (stereotypes.Any(s => s is MethodStereotype.Constructor
                                           or MethodStereotype.CopyConstructor
                                           or MethodStereotype.Destructor)) continue;


                foreach (MethodStereotype s in stereotypes)
                {
                    if (s == MethodStereotype.Get) getters++;
                    if (s.IsAccessor()) accessors++;
                    if (s == MethodStereotype.Set) setters++;
                    if (s is MethodStereotype.Command or MethodStereotype.NonVoidCommand) commands++;
                    if (s == MethodStereotype.Controller) controllers++;
                    if (s.IsCollaborator()) collaborators++;
                    if (s == MethodStereotype.Factory) factories++;
                }
                if (!stereotypes.Any(s => s.IsCollaborator()))
                    nonCollaborators++;

            }
            return new MethodCounts(getters, accessors, setters, commands, controllers, collaborators, factories, nonCollaborators);
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