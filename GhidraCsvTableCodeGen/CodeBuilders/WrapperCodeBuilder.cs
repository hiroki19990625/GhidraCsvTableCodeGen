using CsvHelper;
using GhidraCsvTableCodeGen.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GhidraCsvTableCodeGen.CodeBuilders
{
    public class WrapperCodeBuilder
    {
        private readonly Regex _regex = 
            new Regex(@"\A(?<RetType>[a-zA-z0-9<>,:&]+)\s(?<RetPointer>[*]+)\s?(?<FuncName>[a-zA-z0-9<>,:&]+)\(((?<ArgType>[a-zA-z0-9<>,:&]+)\s(?<ArgPointer>[*]+)\s?(?<ArgName>[a-zA-z0-9<>,:&]+),?\s?)*\)", RegexOptions.Compiled);

        private readonly WrapperCommandOptions _commandOptions;

        public WrapperCodeBuilder(WrapperCommandOptions options)
        {
            _commandOptions = options;
        }

        public void Build()
        {
            IEnumerable<FunctionEntry> entries;

            using (StreamReader reader = new StreamReader(_commandOptions.Input, Encoding.GetEncoding("SHIFT_JIS")))
            using (var csv = new CsvReader(reader, new CultureInfo("ja-JP", false)))
            {
                csv.Configuration.HasHeaderRecord = true;

                entries = csv.GetRecords<FunctionEntry>().ToList();
            }

            Dictionary<string, ClassEntry> clases = new Dictionary<string, ClassEntry>();
            foreach (FunctionEntry function in entries)
            {
                Match match = _regex.Match(function.Signature);
                if (match.Success)
                {
                    string retType = match.Groups["RetType"].Value;
                    string retPointer = match.Groups["RetPointer"].Value;
                    if (!clases.ContainsKey(retType))
                    {
                        clases[retType] = new ClassEntry(retType);
                    }

                    string funcName = match.Groups["FuncName"].Value;
                    string[] argTypes = match.Groups["ArgType"].Captures.Cast<Capture>().Select(e => e.Value).ToArray();
                    string[] argPointers = match.Groups["ArgPointer"].Captures.Cast<Capture>().Select(e => e.Value).ToArray();
                    string[] argNames = match.Groups["ArgName"].Captures.Cast<Capture>().Select(e => e.Value).ToArray();
                }
            }
        }

        class ClassEntry
        {
            public string Name { get; }

            public List<ClassFunctionEntry> Entries { get; } = new List<ClassFunctionEntry>();

            public ClassEntry(string name)
            {
                Name = name;
            }
        }

        class ClassFunctionEntry
        {
            public string RetType { get; }

            public string Name { get; }

            public string[] ParamTypes { get; }

            public ulong Address { get; }
        }
    }
}
