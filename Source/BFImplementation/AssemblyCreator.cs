using System;
using System.Collections.Generic;
using System.IO;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Assembly = System.Reflection.Assembly;
using MethodInfo = System.Reflection.MethodInfo;
using BFOpCode = BFImplementation.OpCode;

namespace BFImplementation
{
    public class AssemblyCreator
    {
        public MethodInfo Create(OpValue[] prog)
        {
            const string OutNamespace = "BF";
            const string OutTypeName = "CompiledProgram";
            const string OutMethodName = "Main";

            var name = new AssemblyNameDefinition(OutNamespace, new Version(1, 0, 0, 0));
            var asm = AssemblyDefinition.CreateAssembly(name, OutNamespace + ".dll", ModuleKind.Dll);

            asm.MainModule.Import(typeof(int));
            var voidret = asm.MainModule.Import(typeof(void));

            var method = new MethodDefinition(OutMethodName,
                                              MethodAttributes.Static | MethodAttributes.Public,
                                              voidret);

            var arrayVar = new VariableDefinition("array", asm.MainModule.Import(typeof(int[])));
            var indexVar = new VariableDefinition("idx", asm.MainModule.Import(typeof(int)));
            
            method.Body.Variables.Add(arrayVar);
            method.Body.Variables.Add(indexVar);

            var ip = method.Body.GetILProcessor();

            ip.Emit(OpCodes.Ldc_I4_0);
            ip.Emit(OpCodes.Stloc, indexVar);

            ip.Emit(OpCodes.Ldc_I4, 300000);
            ip.Emit(OpCodes.Newarr, asm.MainModule.Import(typeof(int)));

            ip.Emit(OpCodes.Stloc, arrayVar);

            Dictionary<int, Instruction> braces = new Dictionary<int, Instruction>();

            for (int i = 0; i < prog.Length; i++)
            {
                Emit(ip, asm.MainModule, arrayVar, indexVar, braces, prog, i);
            }

            ip.Emit(OpCodes.Ret);

            var type = new TypeDefinition(OutNamespace,
                                          OutTypeName,
                                          TypeAttributes.AutoClass | TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit,
                                          asm.MainModule.Import(typeof(object)));

            asm.MainModule.Types.Add(type);
            type.Methods.Add(method);

            asm.EntryPoint = method;

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

        private static void Emit(ILProcessor ip, ModuleDefinition mod, VariableDefinition arrayVar, VariableDefinition indexVar, Dictionary<int, Instruction> braces, OpValue[] ops, int i)
        {
            OpValue op = ops[i];
            switch (op.OpCode)
            {
                case BFOpCode.Increment:
                    ip.Emit(OpCodes.Ldloc, arrayVar);
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldloc, arrayVar);
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldelem_I4);
                    ip.Emit(OpCodes.Ldc_I4, op.Data);
                    ip.Emit(OpCodes.Add);
                    ip.Emit(OpCodes.Stelem_I4);
                    break;
                    
                case BFOpCode.Decrement:
                    ip.Emit(OpCodes.Ldloc, arrayVar);
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldloc, arrayVar);
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldelem_I4);
                    ip.Emit(OpCodes.Ldc_I4, op.Data);
                    ip.Emit(OpCodes.Sub);
                    ip.Emit(OpCodes.Stelem_I4);
                    break;

                case BFOpCode.ShiftRight:
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldc_I4, op.Data);
                    ip.Emit(OpCodes.Add);
                    ip.Emit(OpCodes.Stloc, indexVar);
                    break;
                    
                case BFOpCode.ShiftLeft:
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldc_I4, op.Data);
                    ip.Emit(OpCodes.Sub);
                    ip.Emit(OpCodes.Stloc, indexVar);
                    break;

                case BFOpCode.Output:
                    ip.Emit(OpCodes.Ldloc, arrayVar);
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldelem_I4);
                    ip.Emit(OpCodes.Conv_I1);
                    ip.Emit(OpCodes.Call, mod.Import(typeof(Console).GetMethod("Write", new[] { typeof(char) })));
                    break;

                case BFOpCode.Input:
                    ip.Emit(OpCodes.Call, mod.Import(typeof(Console).GetMethod("Read", new Type[0])));
                    ip.Emit(OpCodes.Ldloc, arrayVar);
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldelem_I4);
                    ip.Emit(OpCodes.Conv_I1);
                    break;

                case BFOpCode.CondLeft:
                    var leftB = ip.Create(OpCodes.Ldloc, arrayVar);
                    var rightB = ip.Create(OpCodes.Nop);
                    ip.Append(leftB);
                    braces[i] = leftB;
                    braces[op.Data] = rightB;
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldelem_I4);
                    ip.Emit(OpCodes.Brfalse, rightB);
                    ip.Emit(OpCodes.Nop);
                    break;

                case BFOpCode.CondRight:
                    ip.Emit(OpCodes.Br, braces[op.Data]);
                    ip.Append(braces[i]);
                    break;

                case BFOpCode.Assign:
                    ip.Emit(OpCodes.Ldloc, arrayVar);
                    ip.Emit(OpCodes.Ldloc, indexVar);
                    ip.Emit(OpCodes.Ldc_I4, op.Data);
                    ip.Emit(OpCodes.Stelem_I4);
                    break;
            }
        }
    }
}

