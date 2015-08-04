using System.Collections.Generic;
using Mono.Cecil;

namespace Sandbox.Fody.Worker
{
    class TypeDefinitionEqualityComparer : IEqualityComparer<TypeDefinition>
    {
        public bool Equals(TypeDefinition x, TypeDefinition y)
        {
            return CecilUtil.TypeMatch(x, y);
        }

        public int GetHashCode(TypeDefinition obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
