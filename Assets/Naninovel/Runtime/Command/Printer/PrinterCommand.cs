namespace Naninovel.Commands
{
    public abstract class PrinterCommand : Command
    {
        protected abstract string AssignedPrinterId { get; }
        protected virtual string AssignedAuthorId => null;

        protected ITextPrinterManager PrinterManager => Engine.GetService<ITextPrinterManager>();
        protected ICharacterManager CharacterManager => Engine.GetService<ICharacterManager>();
        protected TextPrintersConfiguration Configuration => PrinterManager.Configuration;

        public virtual UniTask PreloadResourcesAsync ()
        {
            return GetOrAddPrinterAsync();
        }

        public virtual void ReleasePreloadedResources () { }

        protected virtual async UniTask<ITextPrinterActor> GetOrAddPrinterAsync (AsyncToken asyncToken = default)
        {
            var printerId = default(string);

            if (string.IsNullOrEmpty(AssignedPrinterId) && !string.IsNullOrEmpty(AssignedAuthorId))
                printerId = CharacterManager.Configuration.GetMetadataOrDefault(AssignedAuthorId).LinkedPrinter;

            if (string.IsNullOrEmpty(printerId))
                printerId = AssignedPrinterId;

            var printer = await PrinterManager.GetOrAddActorAsync(printerId ?? PrinterManager.DefaultPrinterId);
            asyncToken.ThrowIfCanceled();
            return printer;
        }
    }
}
