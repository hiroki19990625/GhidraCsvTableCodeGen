using CommandLine;
using CommandLine.Text;
using GhidraCsvTableCodeGen.CodeBuilders;
using GhidraCsvTableCodeGen.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhidraCsvTableCodeGen
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var parseResult = Parser.Default.ParseArguments<TableGeneratorCommandOptions, WrapperCommandOptions>(args);
            parseResult.WithParsed<TableGeneratorCommandOptions>(options =>
            {
                var codeBuilder = new TableGeneratorCodeBuilder(options);
                var code = codeBuilder.Build();

                File.WriteAllText(options.ClassName + ".cs", code);
            })
            .WithParsed<WrapperCommandOptions>(options =>
            {
                var codeBuilder = new WrapperCodeBuilder(options);
                codeBuilder.Build();
            })
            .WithNotParsed(err =>
            {
                var help = HelpText.AutoBuild(parseResult);
                Console.WriteLine($"parse failed {help}");
            });
        }
    }
}