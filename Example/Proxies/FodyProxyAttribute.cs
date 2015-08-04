using System;

namespace Proxies
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class FodyProxyAttribute : Attribute
    {
        public FodyProxyAttribute(Type target)
        {

        }
    }
}
