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
            new Regex(@"\A(?<RetType>[a-zA-z0-9<>,.:&_*()\-]+)\s(?<RetPointer>[*]+)?\s?(?<FuncName>[a-zA-z0-9<>_()\-]+)\(((?<ArgType>[a-zA-z0-9<>,.:&_*()\-]+)\s(?<ArgPointer>[*]+)?\s?(?<ArgName>[a-zA-z0-9<>_.()\-]+),?\s?)*\)", RegexOptions.Compiled);
        private readonly Regex _namespaceRegex = new Regex(@"((?<NameSpace>[a-zA-z0-9<>,.:&_*()\-]+)(::)?)*");

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

            int count = entries.Count();
            int idx = 0;
            Console.WriteLine("Analysis Table {0}", count);
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

                    foreach (string type in argTypes)
                    {
                        if (!clases.ContainsKey(type))
                        {
                            clases[type] = new ClassEntry(type);
                        }
                    }

                    if (argNames.Length > 0 && argNames[0] == "this")
                    {
                        string targetClass = argTypes[0];
                        clases[targetClass].Entries.Add(new ClassFunctionEntry(false, retType, retPointer, funcName, Convert.ToUInt64(function.Location, 16), argNames.Select((e, i) => new ClassFunctionParamEntry(argTypes[i], "", e)).ToArray()));
                    }
                    else
                    {
                        string name = function.Namespace;
                        if (clases.ContainsKey(name))
                            clases[name].Entries.Add(new ClassFunctionEntry(true, retType, retPointer, funcName, Convert.ToUInt64(function.Location, 16), argNames.Select((e, i) => new ClassFunctionParamEntry(argTypes[i], "", e)).ToArray()));
                    }
                }

                Console.WriteLine("Analysis Success {0} / {1}", idx++, count);
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
            public bool IsStatic { get; }

            public string RetType { get; }

            public bool IsRetPointer { get; }

            public string Name { get; }

            public ClassFunctionParamEntry[] Params { get; }

            public ulong Address { get; }

            public ClassFunctionEntry(bool isStatic, string retType, string retPointer, string name, ulong address, ClassFunctionParamEntry[] paramEntries)
            {
                IsStatic = isStatic;
                RetType = retType;
                IsRetPointer = retPointer == "*";

                Name = name;

                Address = address;

                Params = paramEntries;
            }
        }

        class ClassFunctionParamEntry
        {
            string ParamType { get; }
            bool IsParamPointer { get; }
            string ParamName { get; }

            public ClassFunctionParamEntry(string paramType, string paramPointer, string paramName)
            {
                ParamType = paramType;
                IsParamPointer = paramPointer == "*";
                ParamName = paramName;
            }
        }
    }
}
