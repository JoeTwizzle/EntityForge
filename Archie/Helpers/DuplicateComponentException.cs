namespace EntityForge.Helpers
{
    public class DuplicateComponentException : Exception
    {
        readonly string? message;

        public DuplicateComponentException(string? message)
        {
            this.message = message;
        }

        public override string ToString()
        {
            return message + base.ToString();
        }

        public DuplicateComponentException()
        {
        }

        public DuplicateComponentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
