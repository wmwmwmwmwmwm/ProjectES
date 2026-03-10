using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// A <see cref="UITextPrinterPanel"/> implementation for a chat-style printer.
    /// </summary>
    public class ChatPrinterPanel : UITextPrinterPanel, ILocalizableUI
    {
        [System.Serializable]
        public new class GameState
        {
            public List<ChatMessageState> Messages;
            public LocalizableText LastMessageText;
        }

        public override LocalizableText PrintedText { get => printedText; set => SetPrintedText(value); }
        public override string AuthorNameText { get; set; }
        public override float RevealProgress
        {
            get => revealProgress;
            set
            {
                if (value == 0) DestroyAllMessages();
                else if (messageStack?.Count > 0 && messageStack.Peek() is ChatMessage message && message)
                    message.MessageText = lastMessageText;
            }
        }
        public override string Appearance { get; set; }

        protected virtual ScrollRect ScrollRect => scrollRect;
        protected virtual RectTransform MessagesContainer => messagesContainer;
        protected virtual ChatMessage MessagePrototype => messagePrototype;
        protected virtual ScriptableUIBehaviour InputIndicator => inputIndicator;
        protected virtual float RevealDelayModifier => revealDelayModifier;
        protected virtual string ChoiceHandlerId => choiceHandlerId;
        protected virtual RectTransform ChoiceHandlerContainer => choiceHandlerContainer;

        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform messagesContainer;
        [SerializeField] private ChatMessage messagePrototype;
        [SerializeField] private ScriptableUIBehaviour inputIndicator;
        [SerializeField] private float revealDelayModifier = 3f;
        [Tooltip("Associated choice handler actor ID to embed inside the printer; implementation is expected to be MonoBehaviourActor-derived.")]
        [SerializeField] private string choiceHandlerId = "ChatReply";
        [SerializeField] private RectTransform choiceHandlerContainer;

        private readonly Stack<ChatMessage> messageStack = new Stack<ChatMessage>();
        private ICharacterManager characterManager;
        private IChoiceHandlerManager choiceManager;
        private string lastAuthorId;
        private LocalizableText printedText;
        private LocalizableText lastMessageText;
        private float revealProgress = .1f;

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();
            await EmbedChoiceHandlerAsync();
        }

        public override async UniTask RevealPrintedTextOverTimeAsync (float revealDelay, AsyncToken asyncToken)
        {
            var message = AddMessage(lastMessageText, lastAuthorId);
            message.SetIsTyping(true);
            revealProgress = .1f;

            if (revealDelay > 0 && lastMessageText != LocalizableText.Empty)
            {
                await AsyncUtils.WaitEndOfFrameAsync(asyncToken);
                ScrollToBottom(); // Wait before scrolling, otherwise it's not scrolled.
                var revealDuration = ((string)lastMessageText).Count(char.IsLetterOrDigit) * revealDelay * revealDelayModifier;
                var revealStartTime = Engine.Time.Time;
                var revealFinishTime = revealStartTime + revealDuration;
                while (revealFinishTime > Engine.Time.Time && messageStack.Count > 0 && messageStack.Peek() == message)
                {
                    revealProgress = (Engine.Time.Time - revealStartTime) / revealDuration;
                    await AsyncUtils.WaitEndOfFrameAsync(asyncToken);
                    if (asyncToken.Completed) break;
                }
            }

            ScrollToBottom();
            revealProgress = 1f;
            message.SetIsTyping(false);
        }

        public override void SetWaitForInputIndicatorVisible (bool visible)
        {
            if (visible) inputIndicator.Show();
            else inputIndicator.Hide();
        }

        public override void OnAuthorChanged (string authorId, CharacterMetadata authorMeta)
        {
            lastAuthorId = authorId;
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(scrollRect, messagesContainer, messagePrototype, inputIndicator);

            characterManager = Engine.GetService<ICharacterManager>();
            choiceManager = Engine.GetService<IChoiceHandlerManager>();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            if (choiceManager.ActorExists(choiceHandlerId))
                choiceManager.RemoveActor(choiceHandlerId);
        }

        protected virtual async UniTask EmbedChoiceHandlerAsync ()
        {
            if (string.IsNullOrEmpty(ChoiceHandlerId) || !ChoiceHandlerContainer) return;
            var handler = await choiceManager.GetOrAddActorAsync(ChoiceHandlerId) as MonoBehaviourActor<ChoiceHandlerMetadata>;
            if (handler is null || !handler.GameObject) throw new Error($"Choice handler `{ChoiceHandlerId}` is not derived from MonoBehaviourActor or destroyed.");
            var rectTrs = handler.GameObject.GetComponentInChildren<RectTransform>();
            if (!rectTrs) throw new Error($"Choice handler `{ChoiceHandlerId}` is missing RectTransform component.");
            rectTrs.SetParent(ChoiceHandlerContainer, false);
            var ui = ChoiceHandlerContainer.GetComponentInChildren<IManagedUI>();
            if (ui is null) throw new Error($"Choice handler `{ChoiceHandlerId}` is missing IManagedUI component.");
            ui.OnVisibilityChanged += HandleChoiceVisibilityChanged;
        }

        protected virtual void HandleChoiceVisibilityChanged (bool visible)
        {
            ChoiceHandlerContainer.gameObject.SetActive(visible);
            ScrollToBottom();
        }

        protected virtual void SetPrintedText (LocalizableText value)
        {
            printedText = value;

            if (messageStack.Count == 0 || lastMessageText.IsEmpty)
                lastMessageText = value;
            else
            {
                var previousText = messageStack.Peek().MessageText;
                var lastPartIdx = value.Parts.IndexOf(previousText.Parts.Last());
                if (lastPartIdx == -1 || value.Parts.Count <= (lastPartIdx + 1)) return;
                var messageParts = value.Parts.Skip(lastPartIdx + 1).ToArray();
                lastMessageText = new LocalizableText(messageParts);
            }
        }

        protected virtual ChatMessage AddMessage (LocalizableText messageText, string authorId = null, bool instant = false)
        {
            var message = Instantiate(messagePrototype, messagesContainer, false);
            message.MessageText = messageText;
            message.AuthorId = authorId;

            var name = characterManager.GetAuthorName(authorId);
            if (!string.IsNullOrEmpty(name))
            {
                message.ActorNameText = name;
                message.AvatarTexture = CharacterManager.GetAvatarTextureFor(authorId);

                var meta = characterManager.Configuration.GetMetadataOrDefault(authorId);
                if (meta.UseCharacterColor)
                {
                    message.MessageColor = meta.MessageColor;
                    message.ActorNameTextColor = meta.NameColor;
                }
            }
            else
            {
                message.ActorNameText = string.Empty;
                message.AvatarTexture = null;
            }

            if (instant) message.Visible = true;
            else message.Show();

            messageStack.Push(message);
            return message;
        }

        protected virtual void DestroyAllMessages ()
        {
            while (messageStack.Count > 0)
            {
                var message = messageStack.Pop();
                ObjectUtils.DestroyOrImmediate(message.gameObject);
            }
        }

        protected override void SerializeState (GameStateMap stateMap)
        {
            base.SerializeState(stateMap);

            var state = new GameState {
                Messages = messageStack.Select(m => m.GetState()).Reverse().ToList(),
                LastMessageText = lastMessageText
            };
            stateMap.SetState(state, name);
        }

        protected override async UniTask DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            DestroyAllMessages();
            lastMessageText = LocalizableText.Empty;

            var state = stateMap.GetState<GameState>(name);
            if (state is null) return;

            if (state.Messages?.Count > 0)
                foreach (var message in state.Messages)
                    AddMessage(message.PrintedText, message.AuthorId, true);

            lastMessageText = state.LastMessageText;

            ScrollToBottom();
        }

        private async void ScrollToBottom ()
        {
            // Wait a frame and force rebuild layout before setting scroll position,
            // otherwise it's ignoring recently added messages.
            await AsyncUtils.DelayFrameAsync(1);
            if (!scrollRect) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 0;
        }
    }
}
