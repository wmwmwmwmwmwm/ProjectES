using System.Linq;

namespace Naninovel.Commands
{
    /// <summary>
    /// Removes (disposes) actors (character, background, text printer, choice handler) with the specified IDs.
    /// In case multiple actors with the same ID found (eg, a character and a printer), will affect only the first found one.
    /// </summary>
    /// <remarks>
    /// This command should only be used with actor implementations, which don't support per-appearance resource mapping and
    /// only when experiencing issues with memory usage. Consult [memory management](https://pre.naninovel.com/guide/memory-management#actor-resources) guide for more info.
    /// </remarks>
    [CommandAlias("remove")]
    public class RemoveActors : Command, Command.IForceWait
    {
        /// <summary>
        /// IDs of the actors to remove or `*` to remove all actors.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, ActorContext]
        public StringListParameter ActorIds;

        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            if (ShouldRemoveAll()) RemoveAll();
            else RemoveSpecified();
            return UniTask.CompletedTask;
        }

        protected virtual bool ShouldRemoveAll ()
        {
            return ActorIds.FirstOrDefault() == "*";
        }

        protected virtual void RemoveAll ()
        {
            var managers = Engine.FindAllServices<IActorManager>();
            foreach (var manager in managers)
                manager.RemoveAllActors();
        }

        protected virtual void RemoveSpecified ()
        {
            var managers = Engine.FindAllServices<IActorManager>(c => ActorIds.Any(id => c.ActorExists(id)));
            foreach (var actorId in ActorIds)
                if (managers.FirstOrDefault(m => m.ActorExists(actorId)) is IActorManager manager)
                    manager.RemoveActor(actorId);
                else Err($"Failed to remove `{actorId}` actor: can't find any managers with `{actorId}` actor.");
        }
    }
}
