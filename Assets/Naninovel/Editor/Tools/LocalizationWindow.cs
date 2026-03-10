using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naninovel.ManagedText;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class LocalizationWindow : EditorWindow
    {
        protected string SourceScriptsPath
        {
            get => PlayerPrefs.GetString(sourceScriptsPathKey, $"{Application.dataPath}/Scripts");
            set
            {
                PlayerPrefs.SetString(sourceScriptsPathKey, value);
                ValidatePaths();
            }
        }
        protected string SourceManagedTextPath
        {
            get => PlayerPrefs.GetString(sourceManagedTextPathKey, $"{Application.dataPath}/Resources/Naninovel/{Configuration.GetOrDefault<ManagedTextConfiguration>().Loader.PathPrefix}");
            set => PlayerPrefs.SetString(sourceManagedTextPathKey, value);
        }
        protected string LocaleFolderPath
        {
            get => PlayerPrefs.GetString(localeFolderPathKey, $"{Application.dataPath}/Resources/Naninovel/Localization");
            set
            {
                PlayerPrefs.SetString(localeFolderPathKey, value);
                ValidatePaths();
            }
        }
        protected bool Annotate
        {
            get => PlayerPrefs.GetInt(annotatePathKey, 1) == 1;
            set => PlayerPrefs.SetInt(annotatePathKey, value ? 1 : 0);
        }

        private const string sourceScriptsPathKey = "Naninovel." + nameof(LocalizationWindow) + "." + nameof(SourceScriptsPath);
        private const string sourceManagedTextPathKey = "Naninovel." + nameof(LocalizationWindow) + "." + nameof(SourceManagedTextPath);
        private const string localeFolderPathKey = "Naninovel." + nameof(LocalizationWindow) + "." + nameof(LocaleFolderPath);
        private const string annotatePathKey = "Naninovel." + nameof(LocalizationWindow) + "." + nameof(Annotate);
        private const string progressBarTitle = "Generating Localization Resources";

        private static readonly GUIContent sourceScriptsPathContent = new GUIContent("Script Folder (input)", "Folder under which source scripts (.nani) are stored. Alternatively, pick a text folder with the previously generated localization documents to generate based on a non-source locale.");
        private static readonly GUIContent sourceManagedTextPathContent = new GUIContent("Text Folder (input)", "Folder under which source managed text documents are stored ('Resources/Naninovel/Text' by default). Won't generate localization for managed text when not specified.");
        private static readonly GUIContent localeFolderPathContent = new GUIContent("Locale Folder (output)", "The folder for the target locale where to store generated localization resources. Should be inside localization root ('Assets/Resources/Naninovel/Localization' by default) and have a name equal to one of the supported localization tags.");
        private static readonly GUIContent annotateContent = new GUIContent("Include Annotations", "Whether to include source script content and comments placed before it to give translators additional context about the localized content.");
        private static readonly GUIContent warnUntranslatedContent = new GUIContent("Warn Untranslated", "Whether to log warnings when untranslated lines detected while generating localization documents.");
        private static readonly GUIContent spacingContent = new GUIContent("Line Spacing", "Number of empty lines to insert between localized records.");

        private bool localizationRootSelected => availableLocalizations.Count > 0;
        private bool baseOnSourceLocale => sourceTag == l10nConfig.SourceLocale;

        private readonly List<string> availableLocalizations = new List<string>();
        private LocalizationConfiguration l10nConfig;
        private bool warnUntranslated;
        private int wordCount = -1, spacing = 1;
        private bool outputPathValid, sourcePathValid;
        private string targetTag, targetLanguage, sourceTag, sourceLanguage;

        [MenuItem("Naninovel/Tools/Localization")]
        public static void OpenWindow ()
        {
            var position = new Rect(100, 100, 500, 350);
            GetWindowWithRect<LocalizationWindow>(position, true, "Localization", true);
        }

        private void OnEnable ()
        {
            l10nConfig = Configuration.GetOrDefault<LocalizationConfiguration>();
            ValidatePaths();
        }

        private void ValidatePaths ()
        {
            var localizationRoot = l10nConfig.Loader.PathPrefix;

            availableLocalizations.Clear();
            if (LocaleFolderPath != null && Directory.Exists(LocaleFolderPath) && LocaleFolderPath.EndsWithFast(localizationRoot))
                foreach (var locale in Directory.GetDirectories(LocaleFolderPath).Select(Path.GetFileName))
                    if (Languages.ContainsTag(locale))
                        availableLocalizations.Add(locale);

            targetTag = LocaleFolderPath?.GetAfter("/");
            sourceTag = SourceScriptsPath?.GetAfterFirst($"{localizationRoot}/")?.GetBefore("/") ?? l10nConfig.SourceLocale;
            sourcePathValid = Directory.Exists(SourceScriptsPath);
            outputPathValid = localizationRootSelected || (LocaleFolderPath?.GetBeforeLast("/")?.EndsWith(localizationRoot) ?? false) &&
                Languages.ContainsTag(targetTag) && targetTag != l10nConfig.SourceLocale;
            if (outputPathValid)
            {
                targetLanguage = localizationRootSelected ? string.Join(", ", availableLocalizations) : Languages.GetNameByTag(targetTag);
                sourceLanguage = Languages.GetNameByTag(sourceTag);
            }
        }

        private void OnGUI ()
        {
            EditorGUILayout.LabelField("Naninovel Localization", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("The tool to generate localization documents; see Localization guide for usage instructions.", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                SourceScriptsPath = EditorGUILayout.TextField(sourceScriptsPathContent, SourceScriptsPath);
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65)))
                    SourceScriptsPath = EditorUtility.OpenFolderPanel("Source Scripts Folder (input)", "", "");
            }
            if (sourcePathValid)
                EditorGUILayout.HelpBox(sourceLanguage, MessageType.None, false);

            if (baseOnSourceLocale)
            {
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    SourceManagedTextPath = EditorGUILayout.TextField(sourceManagedTextPathContent, SourceManagedTextPath);
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65)))
                        SourceManagedTextPath = EditorUtility.OpenFolderPanel("Source Managed Text Folder (input)", "", "");
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                LocaleFolderPath = EditorGUILayout.TextField(localeFolderPathContent, LocaleFolderPath);
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(65)))
                    LocaleFolderPath = EditorUtility.OpenFolderPanel("Locale Folder Path (output)", "", "");
            }
            if (outputPathValid)
                EditorGUILayout.HelpBox(targetLanguage, MessageType.None, false);

            EditorGUILayout.Space();

            warnUntranslated = EditorGUILayout.Toggle(warnUntranslatedContent, warnUntranslated);
            Annotate = EditorGUILayout.Toggle(annotateContent, Annotate);
            spacing = EditorGUILayout.IntField(spacingContent, spacing);
            GUILayout.FlexibleSpace();

            if (sourcePathValid && outputPathValid)
                EditorGUILayout.HelpBox(wordCount >= 0 ? $"Total localizable words in scenario scripts: {wordCount}." : "Total localizable word count will appear here after the documents are generated.", MessageType.Info);

            if (!sourcePathValid) EditorGUILayout.HelpBox("Script Folder (input) path is not valid. Make sure it points either to folder where naninovel (.nani) scripts are stored or to a folder with the previously generated text localization documents (.txt).", MessageType.Error);
            else if (!outputPathValid)
            {
                if (targetTag == l10nConfig.SourceLocale)
                    EditorGUILayout.HelpBox($"You're trying to create a '{targetTag}' localization, which is equal to the project source locale. That is not allowed; see 'Localization' guide for more info.", MessageType.Error);
                else EditorGUILayout.HelpBox("Locale Folder (output) path is not valid. Make sure it points to the localization root or a subdirectory with name equal to one of the supported language tags.", MessageType.Error);
            }

            EditorGUI.BeginDisabledGroup(!outputPathValid || !sourcePathValid);
            if (GUILayout.Button("Generate Localization Documents", GUIStyles.NavigationButton))
                GenerateLocalizationResources();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
        }

        private void GenerateLocalizationResources ()
        {
            EditorUtility.DisplayProgressBar(progressBarTitle, "Reading source documents...", 0f);

            try
            {
                if (localizationRootSelected)
                    foreach (var locale in availableLocalizations)
                        DoGenerate(Path.Combine(LocaleFolderPath, locale));
                else DoGenerate(LocaleFolderPath);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }

            void DoGenerate (string localeFolderPath)
            {
                LocalizeScripts(localeFolderPath);
                LocalizeManagedText(localeFolderPath);
            }
        }

        private void LocalizeScripts (string localeFolderPath)
        {
            wordCount = 0;
            var missingKeys = new List<string>();
            var outputDirPath = $"{localeFolderPath}/{Configuration.GetOrDefault<ManagedTextConfiguration>().Loader.PathPrefix}/{ManagedTextConfiguration.ScriptMapCategory}";
            if (!Directory.Exists(outputDirPath)) Directory.CreateDirectory(outputDirPath);
            if (baseOnSourceLocale) LocalizeBasedOnScripts();
            else LocalizeBasedOnManagedText();

            void LocalizeBasedOnScripts ()
            {
                EditorUtility.DisplayProgressBar(progressBarTitle, "Finding source script assets...", 0);
                var scriptPaths = Directory.GetFiles(SourceScriptsPath, "*.nani", SearchOption.AllDirectories);
                for (int i = 0; i < scriptPaths.Length; i++)
                {
                    var scriptPath = scriptPaths[i];
                    EditorUtility.DisplayProgressBar(progressBarTitle, $"Processing '{Path.GetFileName(scriptPath)}'...", i / (float)scriptPaths.Length);
                    var script = AssetDatabase.LoadAssetAtPath<Script>(PathUtils.AbsoluteToAssetPath(scriptPath));
                    var category = $"{ManagedTextConfiguration.ScriptMapCategory}/{script.Name}";
                    var outputPath = $"{outputDirPath}/{script.Name}.txt";
                    var existingAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(PathUtils.AbsoluteToAssetPath(outputPath));
                    var existingDoc = existingAsset ? ManagedTextUtils.Parse(existingAsset.text, category, outputPath) : null;
                    var localizer = new ScriptLocalizer(new ScriptLocalizer.Options {
                        Syntax = Compiler.Syntax,
                        OnUntranslated = missingKeys.Add,
                        Annotate = Annotate,
                        AnnotationPrefix = l10nConfig.AnnotationPrefix,
                        Separator = l10nConfig.RecordSeparator[0]
                    });
                    var document = localizer.Localize(script, existingDoc);
                    var documentText = ManagedTextUtils.Serialize(document, category, spacing);
                    var output = PrependHeader(documentText, localeFolderPath, $"'{script.Name}' naninovel script");
                    File.WriteAllText(outputPath, output);
                    AppendWordCount(document);
                    if (warnUntranslated) WarnUntranslated(existingAsset);
                }
            }

            void LocalizeBasedOnManagedText ()
            {
                EditorUtility.DisplayProgressBar(progressBarTitle, "Finding source localization documents...", 0);
                var scriptsFolder = Path.Combine(SourceScriptsPath, ManagedTextConfiguration.ScriptMapCategory);
                if (!Directory.Exists(scriptsFolder))
                {
                    Debug.LogError("Failed to generate localization documents based on non-source locale. Make sure to generate script localization documents for the locale and select managed text root folder of the locale ('Assets/Resources/Naninovel/Localization/{locale}/Text' by default).");
                    return;
                }
                var sourceDocsPaths = Directory.GetFiles(scriptsFolder, "*.txt", SearchOption.AllDirectories);
                for (int i = 0; i < sourceDocsPaths.Length; i++)
                {
                    var sourceDocPath = sourceDocsPaths[i];
                    var sourceDocName = Path.GetFileNameWithoutExtension(sourceDocPath);
                    var category = $"{ManagedTextConfiguration.ScriptMapCategory}/{sourceDocName}";
                    EditorUtility.DisplayProgressBar(progressBarTitle, $"Processing '{sourceDocName}.txt'...", i / (float)sourceDocsPaths.Length);
                    var sourceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(PathUtils.AbsoluteToAssetPath(sourceDocPath));
                    var sourceDoc = ManagedTextUtils.Parse(sourceAsset.text, category, sourceDocPath);
                    var outputPath = $"{outputDirPath}/{sourceDocName}.txt";
                    var existingAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(PathUtils.AbsoluteToAssetPath(outputPath));
                    var existingDoc = existingAsset ? ManagedTextUtils.Parse(existingAsset.text, category, outputPath) : null;
                    var localizer = new ManagedTextLocalizer(new ManagedTextLocalizer.Options {
                        Annotate = Annotate,
                        AnnotationPrefix = l10nConfig.AnnotationPrefix,
                        OnUntranslated = missingKeys.Add
                    });
                    var document = localizer.Localize(sourceDoc, existingDoc);
                    var documentText = ManagedTextUtils.Serialize(document, category, spacing);
                    var output = PrependHeader(documentText, localeFolderPath, $"'{sourceDocName}' naninovel script");
                    File.WriteAllText(outputPath, output);
                    AppendWordCount(document);
                    if (warnUntranslated) WarnUntranslated(existingAsset);
                }
            }

            void WarnUntranslated (TextAsset doc)
            {
                if (!doc || missingKeys.Count == 0) return;
                var lines = doc.text.SplitByNewLine();
                foreach (var key in missingKeys)
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].GetAfterFirst(ManagedTextConstants.RecordMultilineKeyLiteral)?.Trim() != key) continue;
                        Engine.Warn($"{EditorUtils.BuildAssetLink(doc, i + 1)} localization document is missing '{key}' key translation at line #{i + 1}.");
                        break;
                    }
                missingKeys.Clear();
            }

            void AppendWordCount (ManagedTextDocument doc)
            {
                foreach (var record in doc.Records)
                    // string.Split(null) will delimit by whitespace chars; 'default(char[])' is used to prevent ambiguity in case of overloads.
                    wordCount += record.Comment.SplitByNewLine().LastOrDefault()?
                        .Split(default(char[]), StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
            }
        }

        private void LocalizeManagedText (string localeFolderPath)
        {
            if (!Directory.Exists(SourceManagedTextPath)) return;

            var outputPath = $"{localeFolderPath}/{Configuration.GetOrDefault<ManagedTextConfiguration>().Loader.PathPrefix}";
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            var filePaths = Directory.GetFiles(baseOnSourceLocale ? SourceManagedTextPath : SourceScriptsPath, "*.txt", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < filePaths.Length; i++)
            {
                var docPath = filePaths[i];
                var category = Path.GetFileNameWithoutExtension(docPath);
                var targetPath = Path.Combine(outputPath, $"{category}.txt");
                var localizer = new ManagedTextLocalizer(new ManagedTextLocalizer.Options {
                    Annotate = Annotate,
                    AnnotationPrefix = l10nConfig.AnnotationPrefix
                });
                var sourceText = File.ReadAllText(docPath);
                var sourceDoc = ManagedTextUtils.Parse(sourceText, category, docPath);
                var existingText = File.Exists(targetPath) ? File.ReadAllText(targetPath) : null;
                var existingDoc = existingText != null ? ManagedTextUtils.Parse(existingText, category, targetPath) : null;
                var document = localizer.Localize(sourceDoc, existingDoc);
                var documentText = ManagedTextUtils.Serialize(document, category, spacing);
                var output = PrependHeader(documentText, localeFolderPath, $"'{category}' managed text document");
                File.WriteAllText(targetPath, output);
            }
        }

        private string PrependHeader (string documentText, string localeFolderPath, string sourceName)
        {
            var targetTag = Path.GetFileName(localeFolderPath);
            var targetLanguage = Languages.GetNameByTag(targetTag);
            var header = $"{ManagedTextConstants.RecordCommentLiteral}" +
                         $"{sourceLanguage.Remove(" (source)")} <{sourceTag}> to " +
                         $"{targetLanguage} <{targetTag}> localization document for {sourceName}";
            if (!documentText.StartsWithFast("\n")) header += '\n';
            if (!documentText.StartsWithFast("\n\n")) header += '\n';
            return header + documentText;
        }
    }
}
