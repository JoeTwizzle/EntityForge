namespace EntityForge.Helpers
{
    public class MissingComponentException : Exception
    {
        readonly string? message;

        public MissingComponentException(string? message)
        {
            this.message = message;
        }

        public override string ToString()
        {
            return message + base.ToString();
        }

        public MissingComponentException()
        {
        }

        public MissingComponentException(string message, Exception innerException) : base(message, innerException)
        {
            this.message = message;
        }
    }
}
