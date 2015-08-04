using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Sandbox.Fody.Worker
{
    public enum ProxyAccessListType
    {
        Regex, Namespace, NamespaceStart
    }

    public enum ProxyAccessListMode
    {
        Allow, Deny
    }

    public class SandboxAccessList
    {
        struct Entry
        {
            public readonly ProxyAccessListType Type;
            public readonly ProxyAccessListMode Mode;
            public readonly Regex Pattern;
            public readonly string Value;

            public Entry(ProxyAccessListType type, ProxyAccessListMode mode, Regex pattern)
            {
                Mode = mode;
                Type = type;
                Pattern = pattern;
                Value = null;
            }

            public Entry(ProxyAccessListType type, ProxyAccessListMode mode, string value)
            {
                Mode = mode;
                Type = type;
                Pattern = null;
                Value = value;
            }
        }

        private ISandboxLogger _logger;
        private List<Entry> _entries;

        public SandboxAccessList(ISandboxLogger logger)
        {
            _logger = logger;
            _entries = new List<Entry>();
        }

        public void Add(ProxyAccessListType type, ProxyAccessListMode mode, string pattern)
        {
            switch (type)
            {
                case ProxyAccessListType.Regex:
                    Regex regex;
                    try
                    {
                        regex = new Regex(pattern, RegexOptions.Compiled);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Invalid access list pattern: {0}{1}{2}", pattern, Environment.NewLine, e.ToString());
                        return;
                    }

                    _entries.Add(new Entry(type, mode, regex));
                    break;
                case ProxyAccessListType.Namespace:
                case ProxyAccessListType.NamespaceStart:
                    _entries.Add(new Entry(type, mode, pattern));
                    break;
                default:
                    throw new NotSupportedException("ProxyAccessListType");
            }
        }

        public bool IsBlacklisted(TypeDefinition type)
        {
            foreach (var entry in _entries)
            {
                switch (entry.Type)
                {
                    case ProxyAccessListType.Regex:
                        if (!entry.Pattern.IsMatch(type.FullName))
                            continue;
                        break;
                    case ProxyAccessListType.Namespace:
                        if (type.Namespace != entry.Value)
                            continue;
                        break;
                    case ProxyAccessListType.NamespaceStart:
                        if (!type.Namespace.StartsWith(entry.Value))
                            continue;
                        break;
                    default:
                        throw new NotSupportedException("ProxyAccessListType");
                }

                switch (entry.Mode)
                {
                    case ProxyAccessListMode.Allow:
                        return false;
                    case ProxyAccessListMode.Deny:
                        return true;
                    default:
                        throw new NotSupportedException("ProxyAccessListMode");
                }
            }

            return false;
        }

        public bool IsBlacklisted(MethodDefinition method)
        {
            foreach (var entry in _entries)
            {
                switch (entry.Type)
                {
                    case ProxyAccessListType.Regex:
                        if (!entry.Pattern.IsMatch(method.FullName))
                            continue;
                        break;
                    case ProxyAccessListType.Namespace:
                        if (method.DeclaringType.Namespace != entry.Value)
                            continue;
                        break;
                    case ProxyAccessListType.NamespaceStart:
                        if (!method.DeclaringType.Namespace.StartsWith(entry.Value))
                            continue;
                        break;
                    default:
                        throw new NotSupportedException("ProxyAccessListType");
                }

                switch (entry.Mode)
                {
                    case ProxyAccessListMode.Allow:
                        return false;
                    case ProxyAccessListMode.Deny:
                        return true;
                    default:
                        throw new NotSupportedException("ProxyAccessListMode");
                }
            }

            return false;
        }

        public bool IsBlacklisted(FieldDefinition field)
        {
            foreach (var entry in _entries)
            {
                switch (entry.Type)
                {
                    case ProxyAccessListType.Regex:
                        if (!entry.Pattern.IsMatch(field.FullName))
                            continue;
                        break;
                    case ProxyAccessListType.Namespace:
                        if (field.DeclaringType.Namespace != entry.Value)
                            continue;
                        break;
                    case ProxyAccessListType.NamespaceStart:
                        if (!field.DeclaringType.Namespace.StartsWith(entry.Value))
                            continue;
                        break;
                    default:
                        throw new NotSupportedException("ProxyAccessListType");
                }

                switch (entry.Mode)
                {
                    case ProxyAccessListMode.Allow:
                        return false;
                    case ProxyAccessListMode.Deny:
                        return true;
                    default:
                        throw new NotSupportedException("ProxyAccessListMode");
                }
            }

            return false;
        }
    }
}
