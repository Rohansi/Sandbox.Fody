using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Sandbox.Fody.Worker
{
    public class SandboxWorker
    {
        private SandboxAccessListCollection _accessLists;
        private SandboxTypeMap _typeMap;
        private Stack<MemberReference> _context;

        public SandboxWorker(ISandboxLogger logger)
        {
            _context = new Stack<MemberReference>();
            logger = new SandboxContextLogger(logger, _context);

            _accessLists = new SandboxAccessListCollection();
            _typeMap = new SandboxTypeMap(_accessLists, logger);
        }

        public void AddProxyAssembly(AssemblyDefinition assembly)
        {
            _typeMap.AddProxyAssembly(assembly);
        }

        public void AddAccessList(SandboxAccessList accessList)
        {
            _accessLists.Add(accessList);
        }

        public void Process(ModuleDefinition module)
        {
            _typeMap.Module = module;

            foreach (var type in module.GetAllTypes())
            {
                WalkType(type);
            }
        }

        private void WalkType(TypeDefinition type)
        {
            _context.Push(type);

            // base type
            if (type.BaseType != null)
                type.BaseType = _typeMap.Type(type.BaseType);

            // interfaces
            for (var i = 0; i < type.Interfaces.Count; i++)
            {
                type.Interfaces[i] = _typeMap.Type(type.Interfaces[i]);
            }

            // generic parameters
            foreach (var parameter in type.GenericParameters)
            {
                var constraints = parameter.Constraints;
                for (var i = 0; i < constraints.Count; i++)
                {
                    constraints[i] = _typeMap.Type(constraints[i]);
                }
            }

            // fields
            foreach (var field in type.Fields)
            {
                field.FieldType = _typeMap.Type(field.FieldType);
            }

            // methods
            foreach (var method in type.Methods)
            {
                WalkMethod(method);
            }

            _context.Pop();
        }

        private void WalkMethod(MethodDefinition method)
        {
            _context.Push(method);

            // return type
            method.ReturnType = _typeMap.Type(method.ReturnType);

            // parameters
            foreach (var parameter in method.Parameters)
            {
                parameter.ParameterType = _typeMap.Type(parameter.ParameterType);
            }

            // generic parameters
            foreach (var parameter in method.GenericParameters)
            {
                var constraints = parameter.Constraints;
                for (var i = 0; i < constraints.Count; i++)
                {
                    constraints[i] = _typeMap.Type(constraints[i]);
                }
            }

            if (method.HasBody && method.IsIL)
            {
                // TODO: support conversions to/from proxy types?

                // locals
                foreach (var variable in method.Body.Variables)
                {
                    variable.VariableType = _typeMap.Type(variable.VariableType);
                }

                // instructions
                var instructions = method.Body.Instructions;
                foreach (var instr in instructions)
                {
                    if (CecilUtil.IsTypeInstruction(instr))
                    {
                        var typeRef = (TypeReference)instr.Operand;
                        instr.Operand = _typeMap.Type(typeRef);
                        continue;
                    }

                    if (CecilUtil.IsMethodInstruction(instr))
                    {
                        var methodRef = (MethodReference)instr.Operand;
                        instr.Operand = _typeMap.Method(methodRef);
                        continue;
                    }

                    if (CecilUtil.IsFieldInstruction(instr))
                    {
                        var fieldRef = (FieldReference)instr.Operand;
                        instr.Operand = _typeMap.Field(fieldRef);
                        continue;
                    }

                    if (instr.OpCode == OpCodes.Ldtoken)
                    {
                        var typeRef = instr.Operand as TypeReference;
                        if (typeRef != null)
                        {
                            instr.Operand = _typeMap.Type(typeRef);
                            continue;
                        }

                        var methodRef = instr.Operand as MethodReference;
                        if (methodRef != null)
                        {
                            instr.Operand = _typeMap.Method(methodRef);
                            continue;
                        }

                        var fieldRef = instr.Operand as FieldReference;
                        if (fieldRef != null)
                        {
                            instr.Operand = _typeMap.Field(fieldRef);
                        }
                    }
                }
            }

            _context.Pop();
        }
    }
}
