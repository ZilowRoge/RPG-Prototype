namespace OutlineURP
{
    public static class OutlineDebugStats
    {
        public static int LastSetupRenderPassesFrame { get; internal set; } = -1;
        public static int LastAddRenderPassesFrame { get; internal set; } = -1;
        public static int LastMaskRecordGraphFrame { get; internal set; } = -1;
        public static int LastMaskExecuteGraphFrame { get; internal set; } = -1;
        public static int LastMaskExecuteCompatFrame { get; internal set; } = -1;
        public static int LastCompositeRecordGraphFrame { get; internal set; } = -1;
        public static int LastCompositeExecuteGraphFrame { get; internal set; } = -1;
        public static int LastCompositeExecuteCompatFrame { get; internal set; } = -1;
    }
}
