namespace EntityForge.Helpers
{
    public sealed class DuplicateTagException : Exception
    {
        readonly string? message;

        public DuplicateTagException(string? message)
        {
            this.message = message;
        }

        public override string ToString()
        {
            return message + base.ToString();
        }

        public DuplicateTagException()
        {
        }

        public DuplicateTagException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
