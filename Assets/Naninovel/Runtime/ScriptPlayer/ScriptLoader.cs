using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Commands;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptLoader"/>
    [InitializeAtRuntime]
    public class ScriptLoader : IStatefulService<GameStateMap>, IScriptLoader
    {
        [Serializable]
        public class GameState
        {
            public string[] LoadedScripts;
        }

        public event Action<float> OnLoadProgress;

        protected virtual IScriptManager ScriptManager { get; }
        protected virtual ResourcePolicy Policy { get; }
        protected virtual Dictionary<string, ScriptPlaylist> LoadedScriptToList { get; } = new Dictionary<string, ScriptPlaylist>();

        public ScriptLoader (ResourceProviderConfiguration config, IScriptManager scriptManager)
        {
            ScriptManager = scriptManager;
            Policy = config.ResourcePolicy;
        }

        public virtual UniTask InitializeServiceAsync () => UniTask.CompletedTask;

        public virtual void ResetService () => UnloadAll();

        public virtual void DestroyService () => UnloadAll();

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            var state = new GameState {
                LoadedScripts = LoadedScriptToList.Keys.ToArray()
            };
            stateMap.SetState(state);
        }

        public virtual UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>() ?? new GameState {
                LoadedScripts = Array.Empty<string>()
            };
            if (LoadedScriptToList.Count > 0)
                foreach (var orphan in LoadedScriptToList.Keys.Except(state.LoadedScripts).ToArray())
                    UnloadScript(orphan);
            if (state.LoadedScripts.Length == 0) return UniTask.CompletedTask;
            var tasks = new List<UniTask>();
            foreach (var scriptName in state.LoadedScripts)
                if (!IsLoaded(scriptName))
                    tasks.Add(LoadSaved(scriptName, stateMap.PlaybackSpot));
            return UniTask.WhenAll(tasks);
        }

        public virtual async UniTask Load (ScriptPlaylist playlist, int startIndex = 0)
        {
            // In lazy resources are un-/loaded by ScriptPlayer during playback, nothing to do here.
            // Script being already loaded means it was loaded as dependency, so do nothing.
            if (Policy == ResourcePolicy.Lazy || IsLoaded(playlist.ScriptName)) return;
            // In conservative unload after loading to prevent re-loading shared resources.
            if (Policy == ResourcePolicy.Conservative)
            {
                var prevLists = LoadedScriptToList.Values.ToArray();
                LoadedScriptToList.Clear();
                await LoadList(playlist, startIndex);
                await LoadDependencies(playlist);
                foreach (var prevList in prevLists)
                    if (!IsLoaded(prevList.ScriptName))
                        prevList.ReleaseResources();
            }
            // In optimistic loads are sparse, so prefer re-loading shared resources
            // instead of keeping resources from both previous and next script batches
            // while loading.
            else
            {
                UnloadAll();
                await LoadList(playlist, startIndex);
                await LoadDependencies(playlist);
            }
        }

        protected virtual bool IsLoaded (string scriptName)
        {
            if (string.IsNullOrWhiteSpace(scriptName)) return false;
            return LoadedScriptToList.ContainsKey(scriptName);
        }

        protected virtual UniTask LoadList (ScriptPlaylist list, int startIndex)
        {
            if (IsLoaded(list.ScriptName)) return UniTask.CompletedTask;
            LoadedScriptToList.Add(list.ScriptName, list);
            return list.LoadResourcesAsync(startIndex, list.Count - 1, OnLoadProgress);
        }

        protected virtual UniTask LoadSaved (string scriptName, PlaybackSpot playedSpot)
        {
            var list = new ScriptPlaylist(ScriptManager.GetScript(scriptName));
            var startIndex = list.ScriptName == playedSpot.ScriptName
                ? list.GetIndexByLine(playedSpot.LineIndex, playedSpot.InlineIndex) : 0;
            return LoadList(list, startIndex);
        }

        protected virtual UniTask LoadDependencies (ScriptPlaylist list)
        {
            var tasks = new List<UniTask>();
            foreach (var command in list)
                if (TryGetDependency(command, out var scriptName) && !IsLoaded(scriptName))
                    tasks.Add(LoadDependency(scriptName));
            return UniTask.WhenAll(tasks);
        }

        protected virtual async UniTask LoadDependency (string scriptName)
        {
            var list = new ScriptPlaylist(ScriptManager.GetScript(scriptName));
            await LoadDependencies(list);
            await LoadList(list, 0);
        }

        protected virtual bool TryGetDependency (Command command, out string scriptName)
        {
            scriptName = null;
            if (command is Gosub sub) return TryGetScriptDependency(sub.Path, out scriptName);
            if (command is Goto go && IsDependency(go)) return TryGetScriptDependency(go.Path, out scriptName);
            return false;
        }

        protected virtual bool TryGetScriptDependency (NamedStringParameter path, out string scriptName)
        {
            scriptName = null;
            if (path.DynamicValue) return false;
            if (string.IsNullOrWhiteSpace(path.Name)) return false;
            if (path.PlaybackSpot.HasValue && path.Name == path.PlaybackSpot.Value.ScriptName) return false;
            scriptName = path.Name;
            return true;
        }

        protected virtual bool IsDependency (Goto go)
        {
            if (Policy == ResourcePolicy.Optimistic) return !Command.Assigned(go.Release) || !go.Release.Value;
            if (Policy == ResourcePolicy.Conservative) return Command.Assigned(go.Hold) && go.Hold.Value;
            return false;
        }

        protected virtual void UnloadScript (string scriptName)
        {
            LoadedScriptToList[scriptName].ReleaseResources();
            LoadedScriptToList.Remove(scriptName);
        }

        protected virtual void UnloadAll ()
        {
            foreach (var list in LoadedScriptToList.Values)
                list.ReleaseResources();
            LoadedScriptToList.Clear();
        }
    }
}
