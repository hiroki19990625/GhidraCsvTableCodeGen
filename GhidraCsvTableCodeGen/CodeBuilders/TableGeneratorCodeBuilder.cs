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
    public class TableGeneratorCodeBuilder
    {
        private readonly Regex _invalidKeyReplaceRegex =
            new Regex(@"[^0-9a-zA-Z]+", RegexOptions.Compiled);

        private readonly TableGeneratorCommandOptions _commandOptions;

        public TableGeneratorCodeBuilder(TableGeneratorCommandOptions options)
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
                ? new CodeNamespace(_commandOptions.ClassNamespace)
                : new CodeNamespace();
            var @class = new CodeTypeDeclaration(NoConflictSyntaxText(_commandOptions.ClassName));
            @class.IsEnum = _commandOptions.Type == TypeSpec.Enum;
            if (@class.IsEnum)
            {
                @class.BaseTypes.Add(new CodeTypeReference(typeof(long)));
            }

            foreach (FunctionEntry entry in entries)
            {
                var field = new CodeMemberField(typeof(long), SignatureParse(entry.Name, entry.Location));
                field.InitExpression = new CodeSnippetExpression("0x" + entry.Location);
                if (_commandOptions.Type == TypeSpec.Const)
                    field.Attributes = MemberAttributes.Const | MemberAttributes.Public;
                field.Comments.Add(new CodeCommentStatement("<summary>" + EscapeDoc(entry.Signature) + "</summary>", true));

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

        private string SignatureParse(string funcName, string location)
        {
            return MaxLenFix(NoConflictSyntaxText(funcName)) + "_" + location;
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

        private string EscapeDoc(string doc)
        {
            return doc
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("\'", "&apos;");
        }
    }
}