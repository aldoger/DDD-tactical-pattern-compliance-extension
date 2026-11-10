using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainTacticalPatternCompliance.main.CodeValidation
{
    public class Violation
    {
        public string ClassName { get; }    
        public Constraint Constraint { get; }
        public string Message { get; }
        public Violation(
            string className,
            Constraint constraint, 
            string message
        )
        {
            ClassName = className;
            Constraint = constraint;
            Message = message;
        }

        public override string ToString()
        {
            return $"Class: {ClassName} violates constraint: {(int)Constraint}. Description: {Message}";
        }
    }

    public class ViolationList
    {
        public ViolationList() { }
        public List<Violation> Violations { get; } = new List<Violation>();
        public void AddViolation(Violation violation)
        {
            Violations.Add(violation);
        }
    }
}
