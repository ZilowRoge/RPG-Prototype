namespace Common.World.Exams
{
    /// <summary>
    /// Provides a common way to force exam controllers back to their idle state.
    /// </summary>
    public interface IExamResettable
    {
        void ResetExamToIdle();
    }
}
