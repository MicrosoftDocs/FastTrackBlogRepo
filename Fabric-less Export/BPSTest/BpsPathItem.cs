namespace BPSTest
{
    public class BpsPathItem
    {
        public required string Name { get; set; }

        public bool? IsDirectory { get; set; }

        public long? ContentLength { get; set; }

        public DateTime? LastModified { get; set; }
    }
}
