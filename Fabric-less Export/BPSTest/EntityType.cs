namespace BPSTest
{
    public class EntityType
    {
        public EntityType(string name, string location)
        {
            Name = name;
            Location = location;
        }

        public string Name { get; set; }

        public string Location { get; set; }
    }
}
