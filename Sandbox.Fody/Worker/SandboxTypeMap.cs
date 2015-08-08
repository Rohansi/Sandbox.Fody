using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Sandbox.Fody.Worker
{
    class SandboxTypeMap
    {
        private const string ProxyAttribute = "FodyProxyAttribute";

        private ISandboxLogger _logger;
        private Dictionary<TypeDefinition, TypeDefinition> _proxies;
        private SandboxAccessListCollection _accessLists;

        private Dictionary<TypeReference, TypeReference> _typeCache;
        private Dictionary<MethodReference, MethodReference> _methodCache;
        private Dictionary<FieldReference, FieldReference> _fieldCache;

        public SandboxTypeMap(SandboxAccessListCollection accessLists, ISandboxLogger logger)
        {
            _logger = logger;
            _accessLists = accessLists;

            _proxies = new Dictionary<TypeDefinition, TypeDefinition>(new TypeDefinitionEqualityComparer());
            
            _typeCache = new Dictionary<TypeReference, TypeReference>();
            _methodCache = new Dictionary<MethodReference, MethodReference>();
            _fieldCache = new Dictionary<FieldReference, FieldReference>();
        }

        public ModuleDefinition Module { get; set; }

        public int Count
        {
            get { return _proxies.Count; }
        }

        public void AddProxyAssembly(AssemblyDefinition assembly)
        {
            var proxyTypes = assembly.MainModule.GetAllTypes().Where(t => t.HasAttribute(ProxyAttribute));

            foreach (var type in proxyTypes)
            {
                var attrib = type.GetAttribute(ProxyAttribute);
                var targetRef = (TypeReference)attrib.ConstructorArguments[0].Value;
                var targetDef = targetRef.Resolve();

                if (targetDef == null)
                {
                    _logger.LogError("Failed to resolve type '{0}'", targetRef.FullName);
                    continue;
                }

                // TODO: auto-proxy nested types?

                AddProxy(targetDef, type);
            }
        }

        private void AddProxy(TypeDefinition from, TypeDefinition to)
        {
            if (from.IsValueType != to.IsValueType ||
                from.IsInterface != to.IsInterface ||
                from.IsAbstract != to.IsAbstract ||
                from.IsSealed != to.IsSealed ||
                from.GenericParameters.Count != to.GenericParameters.Count)
            {
                _logger.LogError("Type '{0}' is not compatible with '{1}'", from, to);
                return;
            }

            _proxies.Add(from, to);
        }

        public TypeReference Type(TypeReference type)
        {
            if (type.IsGenericParameter)
                return type;

            TypeReference proxyType;
            if (_typeCache.TryGetValue(type, out proxyType))
                return proxyType ?? type;

            var typeDef = type.Resolve();
            if (typeDef == null)
            {
                _logger.LogError("Failed to resolve type '{0}'", type.FullName);
                _typeCache.Add(type, null);
                return type;
            }

            var current = type;
            while (current != null)
            {
                if (current.IsGenericInstance)
                {
                    var currentGen = (GenericInstanceType)current;
                    var arguments = currentGen.GenericArguments;

                    for (var i = 0; i < arguments.Count; i++)
                    {
                        arguments[i] = Type(arguments[i]);
                    }
                }

                current = current.DeclaringType;
            }

            TypeDefinition proxyTypeDef;
            if (!_proxies.TryGetValue(typeDef, out proxyTypeDef))
            {
                if (IsBlacklisted(type))
                    _logger.LogError("Referenced blacklisted type '{0}'", type.FullName);

                _typeCache.Add(type, null);
                return type;
            }

            proxyType = MakeGeneric(Module.Import(proxyTypeDef), type);

            _typeCache.Add(type, proxyType);
            return proxyType;
        }

        public MethodReference Method(MethodReference method)
        {
            MethodReference proxyMethod;
            if (_methodCache.TryGetValue(method, out proxyMethod))
                return proxyMethod ?? method;

            var type = method.DeclaringType;
            var typeDef = type.Resolve();
            if (typeDef == null)
            {
                _logger.LogError("Failed to resolve type '{0}'", type.FullName);
                _methodCache.Add(method, null);
                return method;
            }

            var current = type;
            while (current != null)
            {
                if (current.IsGenericInstance)
                {
                    var currentGen = (GenericInstanceType)current;
                    var arguments = currentGen.GenericArguments;

                    for (var i = 0; i < arguments.Count; i++)
                    {
                        arguments[i] = Type(arguments[i]);
                    }
                }

                current = current.DeclaringType;
            }

            TypeDefinition proxyType;
            if (!_proxies.TryGetValue(typeDef, out proxyType))
            {
                if (IsBlacklisted(method))
                    _logger.LogError("Referenced blacklisted method '{0}'", method.FullName);

                var blacklisted = method.Parameters
                    .Select(p => p.ParameterType)
                    .Concat(Enumerable.Repeat(method.ReturnType, 1))
                    .Concat(method.GenericParameters.SelectMany(p => p.Constraints))
                    .Where(IsBlacklisted);

                foreach (var blacklistedType in blacklisted)
                {
                    _logger.LogError("Method '{0}' references blacklisted type '{1}'", method, blacklistedType);
                }

                _methodCache.Add(method, null);
                return method;
            }
            
            var proxyMethods = proxyType.Methods.Where(m => CecilUtil.MethodMatch(m, method)).ToList();

            if (proxyMethods.Count == 0)
            {
                _logger.LogError("Method '{0}' doesn't exist in its proxy type ('{1}')", method, proxyType);
                _methodCache.Add(method, null);
                return method;
            }

            if (proxyMethods.Count > 1)
            {
                _logger.LogError("Method '{0}' matches multiple methods its proxy type ('{1}')", method, proxyType);
                _methodCache.Add(method, null);
                return method;
            }

            proxyMethod = MakeGeneric(Module.Import(proxyMethods.Single()), method);

            _methodCache.Add(method, proxyMethod);
            return proxyMethod;
        }

        public FieldReference Field(FieldReference field)
        {
            FieldReference proxyField;
            if (_fieldCache.TryGetValue(field, out proxyField))
                return proxyField ?? field;

            var type = field.DeclaringType;
            var typeDef = type.Resolve();
            if (typeDef == null)
            {
                _logger.LogError("Failed to resolve type '{0}'", type.FullName);
                _fieldCache.Add(field, null);
                return field;
            }

            var current = type;
            while (current != null)
            {
                if (current.IsGenericInstance)
                {
                    var currentGen = (GenericInstanceType)current;
                    var arguments = currentGen.GenericArguments;

                    for (var i = 0; i < arguments.Count; i++)
                    {
                        arguments[i] = Type(arguments[i]);
                    }
                }

                current = current.DeclaringType;
            }

            TypeDefinition proxyType;
            if (!_proxies.TryGetValue(typeDef, out proxyType))
            {
                if (IsBlacklisted(field))
                    _logger.LogError("Referenced blacklisted field '{0}'", field.FullName);

                if (IsBlacklisted(field.FieldType))
                    _logger.LogError("Field '{0}' references blacklisted type '{1}'", field, field.FieldType);

                _fieldCache.Add(field, null);
                return field;
            }

            var proxyFields = proxyType.Fields.Where(m => CecilUtil.FieldMatch(m, field)).ToList();

            if (proxyFields.Count == 0)
            {
                _logger.LogError("Field '{0}' doesn't exist in its proxy type ('{1}')", field, proxyType);
                _fieldCache.Add(field, null);
                return field;
            }

            if (proxyFields.Count > 1)
            {
                _logger.LogError("Field '{0}' matches multiple fields its proxy type ('{1}')", field, proxyType);
                _fieldCache.Add(field, null);
                return field;
            }

            proxyField = MakeGeneric(Module.Import(proxyFields.Single()), field);

            _fieldCache.Add(field, proxyField);
            return proxyField;
        }

        private TypeReference MakeGeneric(TypeReference dest, TypeReference src)
        {
            if (!src.IsGenericInstance)
                return dest;

            var srcGen = (GenericInstanceType)src;
            var destGen = new GenericInstanceType(dest);

            foreach (var argument in srcGen.GenericArguments)
            {
                destGen.GenericArguments.Add(argument);
            }

            return destGen;
        }

        private MethodReference MakeGeneric(MethodReference dest, MethodReference src)
        {
            if (src.DeclaringType.IsGenericInstance)
            {
                var newDest = new MethodReference(dest.Name, Type(dest.ReturnType))
                {
                    DeclaringType = MakeGeneric(dest.DeclaringType, src.DeclaringType),
                    HasThis = dest.HasThis,
                    ExplicitThis = dest.ExplicitThis,
                    CallingConvention = dest.CallingConvention
                };

                foreach (var parameter in dest.Parameters)
                {
                    newDest.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, Type(parameter.ParameterType)));
                }

                dest = newDest;
            }

            if (!src.IsGenericInstance)
                return dest;

            var srcGen = (GenericInstanceMethod)src;
            var destGen = new GenericInstanceMethod(dest);

            for (var i = 0; i < srcGen.GenericArguments.Count; i++)
            {
                destGen.GenericArguments.Add(srcGen.GenericArguments[i]);
            }

            return destGen;
        }

        private FieldReference MakeGeneric(FieldReference dest, FieldReference src)
        {
            if (!src.DeclaringType.IsGenericInstance)
                return dest;

            var newDest = new FieldReference(dest.Name, Type(dest.FieldType))
            {
                DeclaringType = MakeGeneric(dest.DeclaringType, src.DeclaringType)
            };

            return newDest;
        }

        private bool IsBlacklisted(TypeReference type)
        {
            if (type.IsGenericParameter) // TODO: verify
                return false;

            var typeDef = type.Resolve();
            if (typeDef == null)
                return true; // TODO: log

            if (ReferenceEquals(Module, typeDef.Module))
                return false;

            return _accessLists.IsBlacklisted(typeDef);
        }

        private bool IsBlacklisted(MethodReference method)
        {
            var methodDef = method.Resolve();
            if (methodDef == null)
                return true; // TODO: log

            if (ReferenceEquals(Module, methodDef.Module))
                return false;

            return _accessLists.IsBlacklisted(methodDef);
        }

        private bool IsBlacklisted(FieldReference field)
        {
            var fieldDef = field.Resolve();
            if (fieldDef == null)
                return true; // TODO: log

            if (ReferenceEquals(Module, fieldDef.Module))
                return false;

            return _accessLists.IsBlacklisted(fieldDef);
        }
    }
}
