using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using Microsoft.CSharp;

namespace GhidraCsvTableCodeGen
{
    public class CodeBuilder
    {
        private readonly Regex _invalidKeyReplaceRegex =
            new Regex(@"[-!#$%&()=~^|+{};*:<>?,./{}@\[\]\`\\ ]", RegexOptions.Compiled);

        private readonly CommandOptions _commandOptions;

        public CodeBuilder(CommandOptions options)
        {
            _commandOptions = options;
        }

        public string Build()
        {
            IEnumerable<FunctionEntry> entries;

            using (StreamReader reader = new StreamReader(_commandOptions.Input, Encoding.GetEncoding("SHIFT_JIS")))
            using (var csv = new CsvReader(reader, new CultureInfo("ja-JP", false)))
            {
                csv.Configuration.HasHeaderRecord = true;

                entries = csv.GetRecords<FunctionEntry>().ToList();
            }

            var unit = new CodeCompileUnit();
            var @namespace = _commandOptions.ClassNamespace != null
                ? new CodeNamespace(NoConflictSyntaxText(_commandOptions.ClassNamespace))
                : new CodeNamespace();
            var @class = new CodeTypeDeclaration(NoConflictSyntaxText(_commandOptions.ClassName));
            @class.IsEnum = _commandOptions.Type == TypeSpec.Enum;

            foreach (FunctionEntry entry in entries)
            {
                var field = new CodeMemberField(typeof(long),
                    SignatureParse(entry.Label, entry.Location, entry.Signature));
                field.InitExpression = new CodeSnippetExpression("0x" + entry.Location);
                if (_commandOptions.Type == TypeSpec.Const)
                    field.Attributes = MemberAttributes.Const | MemberAttributes.Public;

                @class.Members.Add(field);
            }

            @namespace.Types.Add(@class);
            unit.Namespaces.Add(@namespace);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            using (StringWriter writer = new StringWriter())
            {
                provider.GenerateCodeFromCompileUnit(unit, writer, new CodeGeneratorOptions());
                provider.Dispose();

                return writer.ToString();
            }
        }

        public string NoConflictSyntaxText(string value)
        {
            return _invalidKeyReplaceRegex.Replace(value, "_")
                .Replace('"', '_')
                .Replace('\'', '_');
        }

        private string SignatureParse(string label, string location, string signature)
        {
            string[] spaceSplit = signature.Split(' ');
            string[] funcNameAndThis = spaceSplit[1].Split('(');
            string funcName = funcNameAndThis[0];

            if (funcNameAndThis.Length == 2)
            {
                string className = funcNameAndThis[1];
                return MaxLenFix(NoConflictSyntaxText(className) + "_" + NoConflictSyntaxText(funcName)) + "_" +
                       location;
            }

            return MaxLenFix(NoConflictSyntaxText(label)) + "_" + location;
        }

        private string MaxLenFix(string value)
        {
            if (value.Length > 256)
            {
                var str = new string(value.Take(256 - 14).ToArray());
                return str + "_etc";
            }

            return value;
        }
    }
}