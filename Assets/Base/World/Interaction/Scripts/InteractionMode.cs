using System;

namespace Common.World.Interaction
{
    [Flags]
    public enum InteractionMode
    {
        None = 0,
        Trigger = 1 << 0,
        Target = 1 << 1,
        Both = Trigger | Target
    }
}
