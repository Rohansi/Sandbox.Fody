using Sandbox.Fody.Worker;

namespace Sandbox.Fody
{
    class ModuleWeaverLogger : ISandboxLogger
    {
        private ModuleWeaver _weaver;

        public ModuleWeaverLogger(ModuleWeaver weaver)
        {
            _weaver = weaver;
        }

        public void LogError(string format, params object[] args)
        {
            _weaver.LogError(string.Format(format, args));
        }
    }
}
