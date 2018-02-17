using System;
using System.Collections.Generic;
using System.IO;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Assembly = System.Reflection.Assembly;
using AssemblyConfigurationAttribute = System.Reflection.AssemblyConfigurationAttribute;
using ConstructorInfo = System.Reflection.ConstructorInfo;
using MethodInfo = System.Reflection.MethodInfo;

using BFOpCode = BFImplementation.OpCode;

namespace BFImplementation
{
    public class AssemblyCreator
    {
        private static readonly MethodInfo ConsoleWriteChar = typeof(Console).GetMethod("Write", new[] { typeof(char) });
        private static readonly MethodInfo ConsoleRead = typeof(Console).GetMethod("Read", Array.Empty<Type>());
        private static readonly ConstructorInfo CreateAssemblyConfigurationAttribute = typeof(AssemblyConfigurationAttribute).GetConstructor(new[] { typeof(string) });
        private static readonly Type[] ConsoleWriteOverload = { typeof(char) };

        public unsafe MethodInfo Create(OpValue[] prog)
        {
            const string OutNamespace = "BF";
            const string OutTypeName = "CompiledProgram";
            const string OutMethodName = "RunCompiledProgram";

            var name = new AssemblyNameDefinition(OutNamespace, new Version(1, 0, 0, 0));
            var asm = AssemblyDefinition.CreateAssembly(name, OutNamespace + ".dll", ModuleKind.Dll);
            var module = asm.MainModule;

            asm.CustomAttributes.Add(new CustomAttribute(module.ImportReference(CreateAssemblyConfigurationAttribute))
            {
                ConstructorArguments =
                {
                    new CustomAttributeArgument(module.ImportReference(typeof(string)), "Release"),
                },
            });

            var intPtrRef = module.ImportReference(typeof(int*));
            var voidret = module.ImportReference(typeof(void));

            var method = new MethodDefinition(OutMethodName,
                                              MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.Public,
                                              voidret);

            var ptrParam = new ParameterDefinition("ptr", ParameterAttributes.None, intPtrRef);
            method.Parameters.Add(ptrParam);

            var ptrVar = new VariableDefinition(intPtrRef);

            method.Body.Variables.Add(ptrVar);

            var consoleWriteChar = module.ImportReference(ConsoleWriteChar);
            var consoleRead = module.ImportReference(ConsoleRead);

            var ip = method.Body.GetILProcessor();

            ip.Emit(OpCodes.Ldarg_0);
            ip.Emit(OpCodes.Stloc_0);

            Dictionary<int, Instruction> braces = new Dictionary<int, Instruction>();

            for (int i = 0; i < prog.Length; i++)
            {
                Emit(ip, module, consoleWriteChar, consoleRead, braces, prog, i);
            }

            ip.Emit(OpCodes.Ret);

            var type = new TypeDefinition(OutNamespace,
                                          OutTypeName,
                                          TypeAttributes.AnsiClass | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                                          module.ImportReference(typeof(object)));

            module.Types.Add(type);
            type.Methods.Add(method);

            byte[] rawAsm;
            using (var ms = new MemoryStream())
            {
                asm.Write(ms);
                rawAsm = ms.ToArray();
            }

            var reflected = Assembly.Load(rawAsm);
            return reflected.GetType(OutNamespace + "." + OutTypeName)
                            .GetMethod(OutMethodName);
        }

        private static void Emit(ILProcessor ip, ModuleDefinition mod, MethodReference consoleWriteChar, MethodReference consoleRead, Dictionary<int, Instruction> braces, OpValue[] ops, int i)
        {
            OpValue op = ops[i];
            switch (op.OpCode)
            {
                case BFOpCode.ShiftRight:
                case BFOpCode.ShiftLeft:
                    ip.Emit(OpCodes.Ldloc_0);
                    Emit_Ldc_I4(ip, op.Data);
                    ip.Emit(OpCodes.Conv_I);
                    Emit_Ldc_I4(ip, sizeof(int));
                    ip.Emit(OpCodes.Mul);
                    ip.Emit(op.OpCode == BFOpCode.ShiftRight ? OpCodes.Add : OpCodes.Sub);
                    ip.Emit(OpCodes.Stloc_0);
                    break;

                case BFOpCode.Increment:
                case BFOpCode.Decrement:
                    ip.Emit(OpCodes.Ldloc_0);
                    ip.Emit(OpCodes.Dup);
                    ip.Emit(OpCodes.Ldind_I4);
                    Emit_Ldc_I4(ip, op.Data);
                    ip.Emit(op.OpCode == BFOpCode.Increment ? OpCodes.Add : OpCodes.Sub);
                    ip.Emit(OpCodes.Stind_I4);
                    break;

                case BFOpCode.Output:
                    ip.Emit(OpCodes.Ldloc_0);
                    ip.Emit(OpCodes.Ldind_I4);
                    ip.Emit(OpCodes.Conv_U2);
                    ip.Emit(OpCodes.Call, consoleWriteChar);
                    break;

                case BFOpCode.Input:
                    ip.Emit(OpCodes.Ldloc_0);
                    ip.Emit(OpCodes.Call, consoleRead);
                    ip.Emit(OpCodes.Stind_I4);
                    break;

                case BFOpCode.CondLeft:
                    var leftB = ip.Create(OpCodes.Ldloc_0);
                    var rightB = ip.Create(OpCodes.Nop);
                    braces[i] = leftB;
                    braces[op.Data] = rightB;

                    ip.Append(leftB);
                    ip.Emit(OpCodes.Ldind_I4);
                    ip.Emit(OpCodes.Brfalse, rightB);
                    break;

                case BFOpCode.CondRight:
                    ip.Emit(OpCodes.Br, braces[op.Data]);
                    ip.Append(braces[i]);
                    break;

                case BFOpCode.Assign:
                    ip.Emit(OpCodes.Ldloc_0);
                    Emit_Ldc_I4(ip, op.Data);
                    ip.Emit(OpCodes.Stind_I4);
                    break;
            }

            void Emit_Ldc_I4(ILProcessor _ip, int data)
            {
                switch (data)
                {
                    case -1:
                        _ip.Emit(OpCodes.Ldc_I4_M1);
                        return;

                    case 0:
                        _ip.Emit(OpCodes.Ldc_I4_0);
                        return;

                    case 1:
                        _ip.Emit(OpCodes.Ldc_I4_1);
                        return;

                    case 2:
                        _ip.Emit(OpCodes.Ldc_I4_2);
                        return;

                    case 3:
                        _ip.Emit(OpCodes.Ldc_I4_3);
                        return;

                    case 4:
                        _ip.Emit(OpCodes.Ldc_I4_4);
                        return;

                    case 5:
                        _ip.Emit(OpCodes.Ldc_I4_5);
                        return;

                    case 6:
                        _ip.Emit(OpCodes.Ldc_I4_6);
                        return;

                    case 7:
                        _ip.Emit(OpCodes.Ldc_I4_7);
                        return;

                    case 8:
                        _ip.Emit(OpCodes.Ldc_I4_8);
                        return;
                }

                if (data < -128 || data > 127)
                {
                    ip.Emit(OpCodes.Ldc_I4, data);
                }
                else
                {
                    ip.Emit(OpCodes.Ldc_I4_S, unchecked((sbyte)data));
                }
            }
        }
    }
}
