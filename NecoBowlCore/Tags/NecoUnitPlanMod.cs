namespace neco_soft.NecoBowlCore.Tags;

public enum NecoUnitPlanMod
{
    Rotate,
    FlipX,
    FlipY
    
}

public static class NecoUnitPlanModOptions
{
    public struct Rotate
    {
        /// <summary>
        /// Should be between 0-7. Represents number of steps of rotation, counterclockwise.
        /// </summary>
        public int Rotation;
    }

    public struct FlipX
    {
        public bool Enabled;
    }

    public struct FlipY
    {
        public bool Enabled;
    }
}