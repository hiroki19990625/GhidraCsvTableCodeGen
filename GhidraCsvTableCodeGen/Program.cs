using CommandLine;
using CommandLine.Text;
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
            var parserResult = Parser.Default.ParseArguments<CommandOptions>(args);
            await parserResult.MapResult(suc =>
            {
                var codeBuilder = new CodeBuilder(suc);
                var code = codeBuilder.Build();

                File.WriteAllText(suc.ClassName + ".cs", code);

                return Task.CompletedTask;
            }, err =>
            {
                var help = HelpText.AutoBuild(parserResult);
                Console.WriteLine($"parse failed {help}");

                return Task.CompletedTask;
            });
        }
    }
}