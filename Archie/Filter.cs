namespace Archie
{
    public class Filter
    {
        ArchitypePool[] matchingArchitypes;
        public static Filter With<T>() where T : struct
        {
            return new Filter();
        }
        public static Filter Without<T>() where T : struct
        {
            return new Filter();
        }
    }
}