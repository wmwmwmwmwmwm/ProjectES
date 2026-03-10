using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IChoiceHandlerActor"/> implementation using <see cref="UI.ChoiceHandlerPanel"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(ChoiceHandlerPanel), false)]
    public class UIChoiceHandler : MonoBehaviourActor<ChoiceHandlerMetadata>, IChoiceHandlerActor
    {
        public override GameObject GameObject => HandlerPanel.gameObject;
        public override string Appearance { get; set; }
        public override bool Visible { get => HandlerPanel.Visible; set => HandlerPanel.Visible = value; }
        public virtual List<ChoiceState> Choices { get; } = new List<ChoiceState>();

        protected virtual ChoiceHandlerPanel HandlerPanel { get; private set; }

        private readonly IChoiceHandlerManager handlers;
        private readonly IScriptPlayer player;
        private readonly IStateManager state;
        private readonly IUIManager uis;

        public UIChoiceHandler (string id, ChoiceHandlerMetadata meta)
            : base(id, meta)
        {
            handlers = Engine.GetService<IChoiceHandlerManager>();
            player = Engine.GetService<IScriptPlayer>();
            state = Engine.GetService<IStateManager>();
            uis = Engine.GetService<IUIManager>();
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();
            var prefab = await LoadUIPrefabAsync();
            HandlerPanel = await uis.AddUIAsync(prefab, group: BuildActorCategory()) as ChoiceHandlerPanel;
            if (!HandlerPanel) throw new Error($"Failed to initialize `{Id}` choice handler actor: choice panel UI instantiation failed.");
            HandlerPanel.OnChoice -= HandleChoice;
            HandlerPanel.OnChoice += HandleChoice;
            Visible = false;
        }

        public override UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, AsyncToken asyncToken = default)
        {
            return UniTask.CompletedTask;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, AsyncToken asyncToken = default)
        {
            if (HandlerPanel)
                await HandlerPanel.ChangeVisibilityAsync(visible, duration);
        }

        public virtual void AddChoice (ChoiceState choice)
        {
            Choices.Add(choice);
            HandlerPanel.AddChoiceButton(choice);
        }

        public virtual void RemoveChoice (string id)
        {
            Choices.RemoveAll(c => c.Id == id);
            HandlerPanel.RemoveChoiceButton(id);
        }

        public virtual void HandleChoice (string id)
        {
            if (!Choices.Any(c => c.Id == id))
                throw new Error($"Failed to handle choice with ID '{id}': choice not found.");
            HandleChoice(Choices.First(c => c.Id == id));
        }

        public virtual ChoiceState GetChoice (string id) => Choices.FirstOrDefault(c => c.Id == id);

        public override void Dispose ()
        {
            base.Dispose();

            if (HandlerPanel)
            {
                uis.RemoveUI(HandlerPanel);
                ObjectUtils.DestroyOrImmediate(HandlerPanel.gameObject);
                HandlerPanel = null;
            }
        }

        protected virtual async UniTask<GameObject> LoadUIPrefabAsync ()
        {
            var resources = Engine.GetService<IResourceProviderManager>();
            var l10n = Engine.GetService<ILocalizationManager>();
            var resource = await ActorMeta.Loader.CreateLocalizableFor<GameObject>(resources, l10n).LoadAsync(Id);
            if (!resource.Valid) throw new Error($"Failed to load `{Id}` choice handler resource object. Make sure the handler is correctly configured.");
            return resource;
        }

        protected override GameObject CreateHostObject () => null;

        protected override Color GetBehaviourTintColor () => Color.white;

        protected override void SetBehaviourTintColor (Color tintColor) { }

        protected virtual async void HandleChoice (ChoiceState choice)
        {
            if (!Choices.Exists(c => c.Id.EqualsFast(choice.Id))) return;

            state.PeekRollbackStack()?.AllowPlayerRollback();
            AddChoiceToBacklog(choice);
            Choices.Clear();

            if (choice.Nested)
            {
                var continueAt = PlaybackSpot.Invalid;
                if (player.Playing) continueAt = player.PlaybackSpot;
                else
                {
                    var nextIdx = player.Playlist.MoveAt(player.PlayedIndex);
                    if (player.Playlist.IsIndexValid(nextIdx))
                        continueAt = player.Playlist[nextIdx].PlaybackSpot;
                    // Don't throw when next index is invalid, as we may have @goto inside nested callback.
                    // Otherwise a descriptive error is thrown in @choice on exiting the nested callback block.
                }
                handlers.PushPickedChoice(choice.HostedAt, continueAt);
            }

            var scriptText = choice.OnSelectScript;

            if (HandlerPanel)
            {
                HandlerPanel.RemoveAllChoiceButtonsDelayed(); // Delayed to allow custom onClick logic.
                HandlerPanel.Hide();
                if (ActorMeta.WaitHideOnChoice)
                    scriptText = $"@wait {HandlerPanel.FadeTime}\n" + scriptText;
            }

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(GetDestroyCancellationToken()))
            {
                state.OnRollbackStarted += cts.Cancel;
                try { await player.PlayTransient($"`{Id}` on choice script", scriptText, cts.Token); }
                catch (OperationCanceledException) { return; }
                finally
                {
                    if (state != null)
                        state.OnRollbackStarted -= cts.Cancel;
                }
            }

            if (choice.Nested)
                NavigateToNested(choice.HostedAt);
            else if (choice.AutoPlay && !player.Playing)
            {
                var nextIndex = player.PlayedIndex + 1;
                player.Resume(nextIndex);
            }
        }

        protected virtual void AddChoiceToBacklog (ChoiceState state)
        {
            var backlog = uis.GetUI<IBacklogUI>();
            if (backlog == null) return;
            var choices = Choices.Select(c => new BacklogChoice(c.Summary, c.Id == state.Id)).ToArray();
            backlog.AddChoice(choices);
        }

        protected virtual void NavigateToNested (PlaybackSpot hostedAt)
        {
            if (hostedAt.ScriptName != player.PlayedScript.Name)
                throw new Error(Engine.FormatMessage("Choice callback from another script is not supported.", player.PlaybackSpot));
            var index = player.Playlist.IndexOf(hostedAt) + 1;
            if (!player.Playlist.IsIndexValid(index))
                throw new Error(Engine.FormatMessage("Failed navigating to choice callback: playlist index is invalid.", player.PlaybackSpot));
            player.Resume(index);
        }
    }
}
