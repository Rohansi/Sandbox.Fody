using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Sandbox.Fody.Worker
{
    static class CecilUtil
    {
        public static bool HasAttribute(this TypeDefinition type, string attributeTypeName)
        {
            return GetAttribute(type, attributeTypeName) != null;
        }

        public static CustomAttribute GetAttribute(this TypeDefinition type, string attributeTypeName)
        {
            return type.CustomAttributes.FirstOrDefault(t => t.AttributeType.Name == attributeTypeName);
        }

        public static bool TypeMatch(TypeReference a, TypeReference b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a == null || b == null)
                return false;

            if (a.IsGenericParameter)
            {
                if (!b.IsGenericParameter)
                    return false;

                return AreSame((GenericParameter)a, (GenericParameter)b);
            }

            var aSpec = a as TypeSpecification;
            if (aSpec != null)
            {
                var bSpec = b as TypeSpecification;
                if (bSpec == null)
                    return false;

                return AreSame(aSpec, bSpec);
            }

            if (a.Name != b.Name ||
                a.Namespace != b.Namespace ||
                a.HasGenericParameters != b.HasGenericParameters ||
                a.GenericParameters.Count != b.GenericParameters.Count)
            {
                return false;
            }

            return TypeMatch(a.DeclaringType, b.DeclaringType);
        }

        public static bool MethodMatch(MethodReference a, MethodReference b)
        {
            if (a.Name != b.Name ||
                a.HasParameters != b.HasParameters ||
                a.HasGenericParameters != b.HasGenericParameters ||
                (a.HasParameters && a.Parameters.Count != b.Parameters.Count) ||
                (a.HasGenericParameters && a.GenericParameters.Count != b.GenericParameters.Count) ||
                !TypeMatch(a.ReturnType, b.ReturnType))
            {
                return false;
            }

            if (a.HasParameters)
            {
                for (var i = 0; i < a.Parameters.Count; i++)
                {
                    if (!TypeMatch(a.Parameters[i].ParameterType, b.Parameters[i].ParameterType))
                        return false;
                }
            }

            return true;
        }

        public static bool FieldMatch(FieldReference a, FieldReference b)
        {
            return a.Name == b.Name && TypeMatch(a.FieldType, b.FieldType);
        }

        private static bool AreSame(TypeSpecification a, TypeSpecification b)
        {
            if (!TypeMatch(a.ElementType, b.ElementType))
                return false;

            if (a.IsGenericInstance)
            {
                if (!b.IsGenericInstance)
                    return false;

                return AreSame((GenericInstanceType)a, (GenericInstanceType)b);
            }

            if (a.IsRequiredModifier || a.IsOptionalModifier)
                return AreSame((IModifierType)a, (IModifierType)b);

            if (a.IsArray)
            {
                if (!b.IsArray)
                    return false;

                return AreSame((ArrayType)a, (ArrayType)b);
            }

            return true;
        }

        private static bool AreSame(ArrayType a, ArrayType b)
        {
            return a.Rank == b.Rank;
        }

        private static bool AreSame(GenericParameter a, GenericParameter b)
        {
            return a.Position == b.Position;
        }

        private static bool AreSame(IModifierType a, IModifierType b)
        {
            return TypeMatch(a.ModifierType, b.ModifierType);
        }

        public static bool IsTypeInstruction(Instruction instruction)
        {
            return TypeInstructions.Contains(instruction.OpCode);
        }

        public static bool IsMethodInstruction(Instruction instruction)
        {
            return MethodInstructions.Contains(instruction.OpCode);
        }

        public static bool IsFieldInstruction(Instruction instruction)
        {
            return FieldInstructions.Contains(instruction.OpCode);
        }

        #region Instruction Maps

        private static readonly HashSet<OpCode> TypeInstructions = new HashSet<OpCode>
        {
            OpCodes.Box,
            OpCodes.Castclass,
            OpCodes.Constrained,
            OpCodes.Cpobj,
            OpCodes.Initobj,
            OpCodes.Isinst,
            OpCodes.Ldelem_Any,
            OpCodes.Ldelema,
            OpCodes.Ldobj,
            OpCodes.Mkrefany,
            OpCodes.Newarr,
            OpCodes.Refanyval,
            OpCodes.Sizeof,
            OpCodes.Stelem_Any,
            OpCodes.Stobj,
            OpCodes.Unbox,
            OpCodes.Unbox_Any
        };

        private static readonly HashSet<OpCode> MethodInstructions = new HashSet<OpCode>
        {
            OpCodes.Call,
            OpCodes.Callvirt,
            OpCodes.Jmp,
            OpCodes.Ldftn,
            OpCodes.Ldvirtftn,
            OpCodes.Newobj
        };

        private static readonly HashSet<OpCode> FieldInstructions = new HashSet<OpCode>
        {
            OpCodes.Ldfld,
            OpCodes.Ldflda,
            OpCodes.Ldsfld,
            OpCodes.Ldsflda,
            OpCodes.Stfld,
            OpCodes.Stsfld
        }; 

        #endregion
    }
}
