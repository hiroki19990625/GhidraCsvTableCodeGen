﻿using CommandLine;

namespace GhidraCsvTableCodeGen
{
    public class CommandOptions
    {
        [Value(0, MetaName = "Input")] public string Input { get; set; }

        [Value(1, MetaName = "ClassName")] public string ClassName { get; set; }

        [Option('n', "namespace", Default = null, Required = false)]
        public string ClassNamespace { get; set; }

        [Option('t', "type", Default = TypeSpec.Const)]
        public TypeSpec Type { get; set; }
    }
}