namespace EntityForge.Tags
{
    public interface ITag<T> where T : struct, ITag<T>
    {
        /// <summary>
        /// DO NOT MANUALLY ASSIGN
        /// </summary>
        internal static virtual int BitIndex { get; set; }
    }
}
