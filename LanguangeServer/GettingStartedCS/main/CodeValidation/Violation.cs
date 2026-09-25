using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class Violation
    {
        public string className;
        public Constraint Constraint { get; }
        public string Message { get; }
        public Violation(
            string className,
            Constraint constraint, 
            string message
        )
        {
            this.className = className;
            Constraint = constraint;
            Message = message;
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
