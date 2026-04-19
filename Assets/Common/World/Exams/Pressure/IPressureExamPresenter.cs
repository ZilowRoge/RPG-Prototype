namespace Common.World.Exams.Pressure
{
    public interface IPressureExamPresenter
    {
        void HandleExamPreparing(PressureExamController controller);
        void HandleExamStarted(PressureExamController controller);
        void HandleExamFailed(PressureExamController controller, int misses, int maxMisses);
        void HandleExamCompleted(PressureExamController controller, int hits, int misses, int maxMisses);
        void HandleExamAborted(PressureExamController controller);
        void HandleHitCountChanged(int hits);
        void HandleMissCountChanged(int misses, int maxMisses);
        void HandleWaveAdvanced(int currentWave, int totalWaves);
    }
}
