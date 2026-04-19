namespace Common.World.Interaction
{
    public readonly struct InteractionTooltipState
    {
        public InteractionTooltipState(string message, bool isBlocked)
        {
            Message = message ?? string.Empty;
            IsBlocked = isBlocked;
        }

        public string Message { get; }
        public bool IsBlocked { get; }
    }
}
