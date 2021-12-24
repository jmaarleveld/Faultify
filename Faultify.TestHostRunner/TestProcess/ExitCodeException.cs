using System;

namespace Faultify.TestHostRunner.TestProcess
{
    public class ExitCodeException : Exception
    {
        public ExitCodeException(int exitCode) : base($"Process exited with exit code: {exitCode}")
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }
}
