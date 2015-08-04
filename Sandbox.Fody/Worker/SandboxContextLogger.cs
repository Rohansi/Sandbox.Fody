using System.Collections.Generic;
using Mono.Cecil;

namespace Sandbox.Fody.Worker
{
    class SandboxContextLogger : ISandboxLogger
    {
        private ISandboxLogger _logger;
        private Stack<MemberReference> _context;

        public SandboxContextLogger(ISandboxLogger logger, Stack<MemberReference> context)
        {
            _logger = logger;
            _context = context;
        }

        public void LogError(string format, params object[] args)
        {
            var message = string.Format(format, args);

            if (_context.Count == 0)
            {
                _logger.LogError(message);
                return;
            }

            _logger.LogError("{0} in '{1}'", message, _context.Peek().FullName);
        }
    }
}
