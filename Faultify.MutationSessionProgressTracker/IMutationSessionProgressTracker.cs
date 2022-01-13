using System;

namespace Faultify.MutationSessionProgressTracker
{
    public interface IMutationSessionProgressTracker : IProgress<string>
    {
        void LogBeginPreBuilding();
        void LogEndPreBuilding();
        void LogBeginProjectDuplication(int duplications);
        void LogEndProjectDuplication();
        void LogBeginCoverage();

        void LogBeginTestSession(int totalTestRounds, int mutationCount,
            TimeSpan testRunTime);

        void LogTestRunUpdate(int index, int max, int failedRuns);

        void LogEndTestSession(TimeSpan elapsed, int completedTestRounds,
            int mutationCount, float score);

        void LogBeginReportBuilding(string reportType, string reportPath);
        void LogEndFaultify(string processLog);
        void LogDebug(string message);

        void Log(string message,
            LogMessageType logMessageType = LogMessageType.Other);
        
    }
}