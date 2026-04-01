namespace OutlineURP
{
    public enum OutlineOcclusionMode
    {
        RespectDepth = 0,
        XRay = 1
    }

    public enum OutlineGroup
    {
        Enemy = 0,
        Pickup = 1,
        Custom1 = 2,
        Custom2 = 3
    }

    public enum OutlineState
    {
        None = 0,
        Hover = 1,
        Selected = 2
    }
}
