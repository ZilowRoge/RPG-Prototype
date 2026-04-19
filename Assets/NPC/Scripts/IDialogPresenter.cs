namespace NPC.Dialog
{
    public interface IDialogPresenter
    {
        bool IsOpen { get; }
        void Begin(DialogAsset dialog);
        void Close();
    }
}
