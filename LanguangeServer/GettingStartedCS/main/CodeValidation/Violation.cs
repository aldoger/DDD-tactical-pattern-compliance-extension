using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GettingStartedCS.main.CodeValidation
{
    public class Violation
    {
        public Violation(Constraint constraint, string message)
        {
            Constraint = constraint;
            Message = message;
        }
        public Constraint Constraint { get; }
        public string Message { get; }
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
