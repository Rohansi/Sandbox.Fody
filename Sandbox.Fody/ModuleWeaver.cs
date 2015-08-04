using System;
using System.IO;
using System.Xml.Linq;
using Mono.Cecil;
using Sandbox.Fody.Worker;

namespace Sandbox.Fody
{
    public class ModuleWeaver
    {
        public Action<string> LogInfo { get; set; }
        public Action<string> LogWarning { get; set; }
        public Action<string> LogError { get; set; }

        public XElement Config { get; set; }
        public ModuleDefinition ModuleDefinition { get; set; }

        public void Execute()
        {
            if (Config.AttributeValue("Debug") == "true")
                System.Diagnostics.Debugger.Launch();

            var logger = new ModuleWeaverLogger(this);
            var worker = new SandboxWorker(logger);

            var resolver = new DefaultAssemblyResolver();

            var parameters = new ReaderParameters();
            parameters.AssemblyResolver = resolver;

            foreach (var elem in Config.Elements("SearchDirectory"))
            {
                var value = elem.Value;

                if (!Directory.Exists(value))
                {
                    LogWarning(string.Format("Search directory doesn't exist: {0}", value));
                    continue;
                }

                resolver.AddSearchDirectory(elem.Value);
            }

            foreach (var elem in Config.Elements("ProxyAssembly"))
            {
                var value = elem.Value;

                if (!File.Exists(value))
                {
                    LogError(string.Format("Proxy assembly doesn't exist: {0}", value));
                    continue;
                }

                resolver.AddSearchDirectory(Path.GetDirectoryName(value));

                AssemblyDefinition assembly;
                try
                {
                    assembly = AssemblyDefinition.ReadAssembly(value, parameters);
                }
                catch
                {
                    LogError(string.Format("Failed to load proxy assembly: {0}", value));
                    throw;
                }

                worker.AddProxyAssembly(assembly);
            }

            foreach (var list in Config.Elements("AccessList"))
            {
                var accessList = new SandboxAccessList(logger);

                foreach (var elem in list.Elements())
                {
                    var modeName = elem.Name.LocalName;
                    ProxyAccessListMode mode;

                    var typeName = elem.AttributeValue("Type") ?? "Regex";
                    ProxyAccessListType type;

                    if (!Enum.TryParse(typeName, true, out type) || !Enum.IsDefined(typeof(ProxyAccessListType), type))
                    {
                        logger.LogError("Invalid access list entry type: {0}", typeName);
                        continue;
                    }

                    switch (modeName)
                    {
                        case "Allow":
                            mode = ProxyAccessListMode.Allow;
                            break;
                        case "Deny":
                            mode = ProxyAccessListMode.Deny;
                            break;
                        default:
                            logger.LogError("Invalid access list entry: {0}", modeName);
                            continue;
                    }

                    accessList.Add(type, mode, elem.Value);
                }

                worker.AddAccessList(accessList);
            }

            worker.Process(ModuleDefinition);
        }
    }
}
