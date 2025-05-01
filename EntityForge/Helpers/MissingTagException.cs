using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityForge.Helpers
{
    public sealed class MissingTagException : Exception
    {
        readonly string? message;

        public MissingTagException(string? message)
        {
            this.message = message;
        }

        public override string ToString()
        {
            return message + base.ToString();
        }

        public MissingTagException()
        {
        }

        public MissingTagException(string message, Exception innerException) : base(message, innerException)
        {
            this.message = message;
        }
    }
}
