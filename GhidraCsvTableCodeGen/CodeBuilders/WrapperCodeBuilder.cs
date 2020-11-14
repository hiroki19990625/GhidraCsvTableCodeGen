using CsvHelper;
using GhidraCsvTableCodeGen.Options;
using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
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
        private readonly Regex _invalidKeyReplaceRegex =
            new Regex(@"[^0-9a-zA-Z]+", RegexOptions.Compiled);

        private readonly Regex _regex = 
            new Regex(@"\A(?<RetType>[a-zA-z0-9<>,.:&_*()\-]+)\s(?<RetPointer>[*]+)?\s?(?<FuncName>[a-zA-z0-9<>_()\-]+)\(((?<ArgType>[a-zA-z0-9<>,.:&_*()\-]+)\s(?<ArgPointer>[*]+)?\s?(?<ArgName>[a-zA-z0-9<>_.()\-]+),?\s?)*\)", RegexOptions.Compiled);
        private readonly Regex _namespaceRegex = new Regex(@"((?<NameSpace>[a-zA-z0-9<>,.:&_*()\-]+)(::)?)*");

        private readonly WrapperCommandOptions _commandOptions;

        private readonly Dictionary<string, string> _convertTable = new Dictionary<string, string>()
        {
            { "bool", typeof(bool).AssemblyQualifiedName },
            { "char", typeof(char).AssemblyQualifiedName },
            { "byte", typeof(byte).AssemblyQualifiedName },
            { "short", typeof(short).AssemblyQualifiedName },
            { "int", typeof(int).AssemblyQualifiedName },
            { "long", typeof(long).AssemblyQualifiedName },
            { "float", typeof(float).AssemblyQualifiedName },
            { "double", typeof(double).AssemblyQualifiedName },
            { "void", typeof(void).AssemblyQualifiedName },
            { "basic_string_char_struct_std_char_traits_char_class_std_allocator_char_", typeof(string).AssemblyQualifiedName }
        };

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

                Console.WriteLine("Analysis Success {0} / {1}", (idx++) + 1, count);
            }

            Directory.CreateDirectory(_commandOptions.Output);

            idx = 0;
            count = clases.Count;
            foreach (ClassEntry entry in clases.Values)
            {
                string name = NoConflictSyntaxText(entry.Name);
                if (name.StartsWith("_") || char.IsLower(name[0]))
                    continue;

                var unit = new CodeCompileUnit();
                var @namespace = _commandOptions.ClassNamespace != null
                    ? new CodeNamespace(_commandOptions.ClassNamespace)
                    : new CodeNamespace();
                var @class = new CodeTypeDeclaration(name);
                @class.Attributes = MemberAttributes.Public;
                foreach (ClassFunctionEntry functionEntry in entry.Entries)
                {
                    var method = new CodeMemberMethod();
                    method.Name = NoConflictSyntaxText(functionEntry.Name);
                    method.ReturnType = CreateTypeRefType(NoConflictSyntaxText(functionEntry.RetType));
                    method.Attributes = MemberAttributes.Public;

                    var dg = new CodeTypeDelegate("_" + method.Name + "_" + functionEntry.Address.ToString("x"));
                    dg.ReturnType = CreateTypeRefType(NoConflictSyntaxText(functionEntry.RetType));
                    dg.Attributes = MemberAttributes.Public;

                    foreach (ClassFunctionParamEntry paramEntry in functionEntry.Params)
                    {
                        var param = new CodeParameterDeclarationExpression(CreateTypeRefType(NoConflictSyntaxText(paramEntry.ParamType)), NoConflictSyntaxText(paramEntry.ParamName));
                        method.Parameters.Add(param);
                        dg.Parameters.Add(param);
                    }

                    method.Statements.Add(new CodeSnippetStatement("        " + string.Format(_commandOptions.DllCallFunction, "0x" + functionEntry.Address.ToString("x"), dg.Name, string.Join(", ", method.Parameters.Cast<CodeParameterDeclarationExpression>().Select(e => e.Name)))));

                    @class.Members.Add(method);
                    @class.Members.Add(dg);
                }

                @namespace.Types.Add(@class);
                unit.Namespaces.Add(@namespace);

                CSharpCodeProvider provider = new CSharpCodeProvider();
                using (StringWriter writer = new StringWriter())
                {
                    provider.GenerateCodeFromCompileUnit(unit, writer, new CodeGeneratorOptions());
                    provider.Dispose();

                    File.WriteAllText(_commandOptions.Output + "/" + MaxLenFix(@class.Name) + ".cs", writer.ToString());
                }

                Console.WriteLine("Generate Class Success {0} / {1}", (idx++) + 1, count);
            }
        }

        public string NoConflictSyntaxText(string value)
        {
            return _invalidKeyReplaceRegex.Replace(value, "_")
                .Replace('"', '_')
                .Replace('\'', '_');
        }
        private string MaxLenFix(string value)
        {
            if (value.Length > 60)
            {
                var str = new string(value.Take(60).ToArray());
                return str + "_etc";
            }

            return value;
        }

        public CodeTypeReference CreateTypeRefType(string type)
        {
            if (_convertTable.ContainsKey(type))
            {
                return new CodeTypeReference(Type.GetType(_convertTable[type]));
            }

            return new CodeTypeReference(type);
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
            public string ParamType { get; }
            public bool IsParamPointer { get; }
            public string ParamName { get; }

            public ClassFunctionParamEntry(string paramType, string paramPointer, string paramName)
            {
                ParamType = paramType;
                IsParamPointer = paramPointer == "*";
                ParamName = paramName;
            }
        }
    }
}
