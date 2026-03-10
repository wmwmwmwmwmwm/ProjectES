using System.Collections.Generic;
using System.IO;
using Naninovel.ManagedText;
using static Naninovel.Spreadsheet.Constants;

namespace Naninovel.Spreadsheet
{
    public class ScriptExporter
    {
        protected virtual string ScriptFolder { get; }
        protected virtual string L10nFolder { get; }
        protected virtual string OutputFolder { get; }
        protected virtual string SourceLocale { get; }
        protected virtual bool Annotate { get; }
        protected virtual MultilineManagedTextParser TextParser { get; } = new MultilineManagedTextParser();
        protected virtual List<SheetColumn> Columns { get; } = new List<SheetColumn>();
        protected virtual Dictionary<string, int> KeyToIndex { get; } = new Dictionary<string, int>();
        protected virtual List<string> Cells { get; } = new List<string>();
        protected virtual SheetWriter SheetWriter { get; } = new SheetWriter();

        public ScriptExporter (string scriptFolder, string l10nFolder,
            string outputFolder, string sourceLocale, bool annotate)
        {
            ScriptFolder = scriptFolder;
            L10nFolder = l10nFolder;
            OutputFolder = outputFolder;
            SourceLocale = sourceLocale;
            Annotate = annotate;
        }

        public virtual void Export (string scriptPath)
        {
            Reset();
            var script = LoadScript(scriptPath);
            var document = CreateDocument(script);
            var sheet = BuildSheet(script.Name, document);
            WriteSheet(sheet, scriptPath);
        }

        protected virtual void Reset ()
        {
            Columns.Clear();
            KeyToIndex.Clear();
            Cells.Clear();
        }

        protected virtual Script LoadScript (string scriptPath)
        {
            var name = Path.GetFileNameWithoutExtension(scriptPath);
            var text = File.ReadAllText(scriptPath);
            return Script.FromText(name, text, scriptPath);
        }

        protected virtual ManagedTextDocument CreateDocument (Script script)
        {
            var cfg = Configuration.GetOrDefault<LocalizationConfiguration>();
            var localizer = new ScriptLocalizer(new ScriptLocalizer.Options {
                Syntax = Compiler.Syntax,
                Annotate = Annotate,
                AnnotationPrefix = "",
                Separator = cfg.RecordSeparator[0]
            });
            return localizer.Localize(script);
        }

        protected virtual Sheet BuildSheet (string scriptName, ManagedTextDocument doc)
        {
            AppendKeysColumnAndIndexKeys(doc);
            if (Annotate) AppendAnnotationsColumn(doc);
            AppendSourceColumn(doc);
            foreach (var localeFolder in Directory.EnumerateDirectories(L10nFolder))
                AppendLocaleColumn(PathUtils.FormatPath(localeFolder), scriptName);
            return new Sheet(Columns.ToArray());
        }

        protected virtual void WriteSheet (Sheet sheet, string scriptPath)
        {
            var localPath = scriptPath.GetBetween(ScriptFolder + '/', ScriptExtension) + CsvExtension;
            var path = PathUtils.Combine(OutputFolder, ScriptFolderName, localPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            SheetWriter.Write(sheet, path);
        }

        protected virtual void AppendKeysColumnAndIndexKeys (ManagedTextDocument doc)
        {
            Cells.Clear();
            Cells.Add(KeysColumnHeader);
            foreach (var record in doc.Records)
            {
                KeyToIndex[record.Key] = Cells.Count;
                Cells.Add(record.Key);
            }
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AppendAnnotationsColumn (ManagedTextDocument doc)
        {
            Cells.Clear();
            Cells.Add(AnnotationsColumnHeader);
            foreach (var record in doc.Records)
                AssignCell(record.Key, GetAnnotation(record));
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AppendSourceColumn (ManagedTextDocument doc)
        {
            Cells.Clear();
            Cells.Add(SourceLocale);
            foreach (var record in doc.Records)
                AssignCell(record.Key, GetLocalizedText(record));
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AppendLocaleColumn (string localeFolder, string scriptName)
        {
            if (!TryLoadL10nDocument(localeFolder, scriptName, out var doc)) return;
            Cells.Clear();
            Cells.Add(localeFolder.GetAfter("/"));
            foreach (var record in doc.Records)
                AssignCell(record.Key, record.Value);
            Columns.Add(new SheetColumn(Cells.ToArray()));
        }

        protected virtual void AssignCell (string key, string value)
        {
            if (KeyToIndex.TryGetValue(key, out var index))
                Cells.Insert(index, value);
            else Engine.Warn($"Failed to assign {value} to {key} while exporting script.");
        }

        protected virtual bool TryLoadL10nDocument (string localeFolder, string scriptName, out ManagedTextDocument doc)
        {
            var path = PathUtils.Combine(localeFolder, TextFolderName, ScriptFolderName, scriptName + TextExtension);
            if (!File.Exists(path))
            {
                Engine.Warn($"Failed to load {path}: make sure localization is generated.");
                doc = null;
                return false;
            }
            doc = TextParser.Parse(File.ReadAllText(path));
            return true;
        }

        protected virtual string GetAnnotation (ManagedTextRecord record)
        {
            return record.Comment.GetBeforeLast("\n") ?? "";
        }

        protected virtual string GetLocalizedText (ManagedTextRecord record)
        {
            return record.Comment.GetAfter("\n") ?? record.Comment;
        }
    }
}
