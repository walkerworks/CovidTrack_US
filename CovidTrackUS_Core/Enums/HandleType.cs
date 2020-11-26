namespace CovidTrackUS_Core.Enums
{
    /// <summary>
    /// A struct to act *like an enum for the ways we 
    /// can notify subscribers
    /// </summary>
    public struct HandleType
    {
        string value;

        public static HandleType Phone => "PHONE";
        public static HandleType Email => "EMAIL";

        private HandleType(string value)
        {
            this.value = value;
        }

        public static implicit operator HandleType(string value)
        {
            return new HandleType(value);
        }

        public static implicit operator string(HandleType handleType)
        {
            return handleType.value;
        }
    }
}
