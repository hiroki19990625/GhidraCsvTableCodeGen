using CsvHelper.Configuration.Attributes;

namespace GhidraCsvTableCodeGen
{
    public class FunctionEntry
    {
        [Index(0)] public string Label { get; set; }
        [Index(1)] public string Location { get; set; }
        [Index(2)] public string Signature { get; set; }
        [Index(3)] public string Name { get; set; }
        [Index(4)] public string Size { get; set; }
    }
}