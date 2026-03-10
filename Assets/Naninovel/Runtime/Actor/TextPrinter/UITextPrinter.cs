using Naninovel.UI;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ITextPrinterActor"/> implementation using <see cref="UITextPrinterPanel"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(UITextPrinterPanel), false)]
    public class UITextPrinter : MonoBehaviourActor<TextPrinterMetadata>, ITextPrinterActor
    {
        public override GameObject GameObject => PrinterPanel.gameObject;
        public override string Appearance { get => PrinterPanel.Appearance; set => PrinterPanel.Appearance = value; }
        public override bool Visible { get => PrinterPanel.Visible; set => PrinterPanel.Visible = value; }
        public virtual LocalizableText Text { get => text; set => SetText(value); }
        public virtual AuthorInfo Author { get => author; set => SetAuthor(value); }
        public virtual List<string> RichTextTags { get => richTextTags; set => SetRichTextTags(value); }
        public virtual float RevealProgress { get => PrinterPanel.RevealProgress; set => SetRevealProgress(value); }
        public virtual UITextPrinterPanel PrinterPanel { get; private set; }

        protected virtual bool UsingRichTags => richTextTags.Count > 0;

        private readonly List<string> richTextTags = new List<string>();
        private readonly IUIManager uiManager;
        private readonly ICharacterManager charManager;
        private readonly ILocalizationManager l10n;
        private readonly AspectMonitor aspectMonitor;
        private LocalizableText text;
        private AuthorInfo author;
        private CancellationTokenSource revealTextCTS;
        private string activeOpenTags, activeCloseTags;

        public UITextPrinter (string id, TextPrinterMetadata meta)
            : base(id, meta)
        {
            uiManager = Engine.GetService<IUIManager>();
            charManager = Engine.GetService<ICharacterManager>();
            l10n = Engine.GetService<ILocalizationManager>();
            activeOpenTags = string.Empty;
            activeCloseTags = string.Empty;
            aspectMonitor = new AspectMonitor();
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();
            var prefab = await LoadUIPrefabAsync();
            PrinterPanel = await uiManager.AddUIAsync(prefab, group: BuildActorCategory()) as UITextPrinterPanel;
            if (!PrinterPanel) throw new Error($"Failed to initialize `{Id}` printer actor: printer panel UI instantiation failed.");
            PrinterPanel.PrintedText = LocalizableText.Empty;
            RevealProgress = 0f;
            aspectMonitor.OnChanged += HandleAspectChanged;
            aspectMonitor.Start(target: PrinterPanel);
            l10n.OnLocaleChanged += HandleLocaleChanged;
            SetAuthor(default);
            Visible = false;
        }

        public override UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, AsyncToken asyncToken = default)
        {
            Appearance = appearance;
            return UniTask.CompletedTask;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, AsyncToken asyncToken = default)
        {
            await PrinterPanel.ChangeVisibilityAsync(visible, duration, asyncToken);
        }

        public virtual async UniTask RevealTextAsync (float revealDelay, AsyncToken asyncToken = default)
        {
            CancelRevealTextRoutine();
            revealTextCTS = CancellationTokenSource.CreateLinkedTokenSource(asyncToken.CancellationToken);
            var revealTextToken = new AsyncToken(revealTextCTS.Token, asyncToken.CompletionToken);
            await PrinterPanel.RevealPrintedTextOverTimeAsync(revealDelay, revealTextToken);
        }

        public override void Dispose ()
        {
            base.Dispose();

            aspectMonitor?.Stop();
            CancelRevealTextRoutine();

            if (PrinterPanel)
            {
                uiManager.RemoveUI(PrinterPanel);
                ObjectUtils.DestroyOrImmediate(PrinterPanel.gameObject);
                PrinterPanel = null;
            }

            if (l10n != null)
                l10n.OnLocaleChanged -= HandleLocaleChanged;
        }

        protected virtual void SetRichTextTags (List<string> tags)
        {
            richTextTags.Clear();

            if (tags?.Count > 0)
                richTextTags.AddRange(tags);

            if (UsingRichTags)
            {
                activeOpenTags = GetActiveTagsOpenSequence();
                activeCloseTags = GetActiveTagsCloseSequence();
            }
            else
            {
                activeOpenTags = string.Empty;
                activeCloseTags = string.Empty;
            }

            SetText(Text); // Update the printed text with the tags.
        }

        protected virtual void SetAuthor (AuthorInfo author)
        {
            this.author = author;
            PrinterPanel.AuthorNameText = author.Label.IsEmpty ? charManager.GetAuthorName(author.Id) : (string)author.Label;
            var authorMeta = charManager.Configuration.GetMetadataOrDefault(author.Id);
            PrinterPanel.OnAuthorChanged(author.Id, authorMeta);
        }

        protected virtual async Task<GameObject> LoadUIPrefabAsync ()
        {
            var providerManager = Engine.GetService<IResourceProviderManager>();
            var localizationManager = Engine.GetService<ILocalizationManager>();
            var resource = await ActorMeta.Loader.CreateLocalizableFor<GameObject>(providerManager, localizationManager).LoadAsync(Id);
            if (!resource.Valid) throw new Error($"Failed to load `{Id}` UI text printer resource object. Make sure the printer is correctly configured.");
            return resource;
        }

        protected override GameObject CreateHostObject () => null;

        protected virtual void SetRevealProgress (float value)
        {
            CancelRevealTextRoutine();
            PrinterPanel.RevealProgress = value;
        }

        protected override Vector3 GetBehaviourPosition ()
        {
            if (!PrinterPanel || !PrinterPanel.Content) return Vector3.zero;
            return PrinterPanel.Content.position;
        }

        protected override void SetBehaviourPosition (Vector3 position)
        {
            if (!PrinterPanel || !PrinterPanel.Content) return;
            PrinterPanel.Content.localPosition = (Vector2)position; // don't change z-pos, as it'll break UI ordering
        }

        protected override Quaternion GetBehaviourRotation ()
        {
            if (!PrinterPanel || !PrinterPanel.Content) return Quaternion.identity;
            return PrinterPanel.Content.rotation;
        }

        protected override void SetBehaviourRotation (Quaternion rotation)
        {
            if (!PrinterPanel || !PrinterPanel.Content) return;
            PrinterPanel.Content.rotation = rotation;
        }

        protected override Vector3 GetBehaviourScale ()
        {
            if (!PrinterPanel || !PrinterPanel.Content) return Vector3.one;
            return PrinterPanel.Content.localScale;
        }

        protected override void SetBehaviourScale (Vector3 scale)
        {
            if (!PrinterPanel || !PrinterPanel.Content) return;
            PrinterPanel.Content.localScale = scale;
        }

        protected override Color GetBehaviourTintColor () => PrinterPanel.TintColor;

        protected override void SetBehaviourTintColor (Color value) => PrinterPanel.TintColor = value;

        protected virtual void SetText (LocalizableText value)
        {
            text = value;
            // Handle rich text tags before assigning the actual text.
            PrinterPanel.PrintedText = UsingRichTags ? (activeOpenTags + text + activeCloseTags) : text;
        }

        protected virtual void HandleAspectChanged (AspectMonitor monitor)
        {
            // UI printers anchored to canvas borders are moved on aspect change;
            // re-set position here to return them to correct relative positions.
            SetBehaviourPosition(GetBehaviourPosition());
        }

        protected virtual void HandleLocaleChanged (string _)
        {
            SetAuthor(Author);
            SetText(Text);
        }

        private void CancelRevealTextRoutine ()
        {
            revealTextCTS?.Cancel();
            revealTextCTS?.Dispose();
            revealTextCTS = null;
        }

        private string GetActiveTagsOpenSequence ()
        {
            var result = string.Empty;

            if (RichTextTags is null || RichTextTags.Count == 0)
                return result;

            foreach (var tag in RichTextTags)
                result += $"<{tag}>";

            return result;
        }

        private string GetActiveTagsCloseSequence ()
        {
            var result = string.Empty;

            if (RichTextTags is null || RichTextTags.Count == 0)
                return result;

            var reversedActiveTags = RichTextTags;
            reversedActiveTags.Reverse();
            foreach (var tag in reversedActiveTags)
                result += $"</{tag.GetBefore("=") ?? tag}>";

            return result;
        }
    }
}
