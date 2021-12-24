using System.Diagnostics;
using System.Threading.Tasks;
using NLog;

namespace Faultify.TestHostRunner.TestProcess
{
    /// <summary>
    ///     Async process runner.
    /// </summary>
    public class ProcessRunner
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ProcessStartInfo _processStartInfo;

        public ProcessRunner(ProcessStartInfo processStartInfo)
        {
            _processStartInfo = processStartInfo;
        }

        /// <summary>
        ///     Runs the process asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task<Process> RunAsync()
        {
            Logger.Info("Starting new process");
            Process? process = new Process();

            TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (o, e) => { taskCompletionSource.TrySetResult(null); };

            process.StartInfo = _processStartInfo;

            process.OutputDataReceived += (sender, e) => { if (e.Data != null) Logger.Debug(e.Data); };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Logger.Error(e.Data); };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();


            await taskCompletionSource.Task;

            process.WaitForExit();

            return process;
        }
    }
}
