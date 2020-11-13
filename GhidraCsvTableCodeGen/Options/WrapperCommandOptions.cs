using CommandLine;

namespace GhidraCsvTableCodeGen.Options
{
    [Verb("wrapper")]
    public class WrapperCommandOptions
    {
        [Value(0, MetaName = "Input")] public string Input { get; set; }
        [Value(1, MetaName = "Directory Output")] public string Output { get; set; }

        [Option('n', "namespace", Default = null, Required = false)]
        public string ClassNamespace { get; set; }

        [Option('d', "dllcallfunction")]
        public string DllCallFunction { get; set; }
    }
}
