using UnityEngine;
using Player.Interfaces;

namespace Common.UI
{
    public interface IPlayerUiBinder
    {
        void BindPlayer(GameObject player);
    }

    public interface IDialogueProgressReceiver
    {
        void BindDialogueProgress(IDialogueProgressContext progressContext);
    }
}
