namespace Observatory.Framework.Files.ParameterTypes
{
    public class Destination
    {
        public ulong System { get; init; }
        public int Body { get; init; }
        public string Name { get; init; }
        public string Name_Localised { get; init; }

        public string SpokenName => Name_Localised ?? Name;
    }
}
