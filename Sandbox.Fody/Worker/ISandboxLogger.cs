namespace Sandbox.Fody.Worker
{
    public interface ISandboxLogger
    {
        void LogError(string format, params object[] args);
    }
}
