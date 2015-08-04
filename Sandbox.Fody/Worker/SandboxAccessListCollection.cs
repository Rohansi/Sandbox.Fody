using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Sandbox.Fody.Worker
{
    class SandboxAccessListCollection
    {
        private List<SandboxAccessList> _accessLists;

        public SandboxAccessListCollection()
        {
            _accessLists = new List<SandboxAccessList>();
        }

        public void Add(SandboxAccessList accessList)
        {
            _accessLists.Add(accessList);
        }

        public bool IsBlacklisted(TypeDefinition type)
        {
            return _accessLists.Any(a => a.IsBlacklisted(type));
        }

        public bool IsBlacklisted(MethodDefinition method)
        {
            return _accessLists.Any(a => a.IsBlacklisted(method));
        }

        public bool IsBlacklisted(FieldDefinition field)
        {
            return _accessLists.Any(a => a.IsBlacklisted(field));
        }
    }
}
