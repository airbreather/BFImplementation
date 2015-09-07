using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BFImplementation
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: 1 argument, which should be the path to a BF source file.");
                return 1;
            }

            string path = args[0];
            string progText;
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                progText = reader.ReadToEnd();
            }

            OpValue[] prog = new Compiler().Compile(progText);

            RunCompiled(prog);
            RunJIT(prog);
            RunInterpreter(prog);

            return 0;
        }

        private static void RunCompiled(OpValue[] prog)
        {
            Stopwatch sw = Stopwatch.StartNew();
            string exe = new NativeCreator().Create(prog);
            long compileTimeTicks = sw.ElapsedTicks;

            using (var process = Process.Start(exe))
            {
                process.WaitForExit();
                sw.Stop();
            }

            Console.WriteLine("Compiled to " + exe);

            double freq = (double)Stopwatch.Frequency;
            Console.WriteLine("Time to compile and run: {0:N5} seconds", sw.ElapsedTicks / freq);
            Console.WriteLine("Time to compile:         {0:N5} seconds", compileTimeTicks / freq);
            Console.WriteLine("Time to run:             {0:N5} seconds", (sw.ElapsedTicks - compileTimeTicks) / freq);
        }

        private static void RunJIT(OpValue[] prog)
        {
            Stopwatch sw = Stopwatch.StartNew();
            System.Reflection.MethodInfo compiled = new AssemblyCreator().Create(prog);
            
            long compileTimeTicks = sw.ElapsedTicks;

            compiled.Invoke(null, null);
            sw.Stop();

            double freq = (double)Stopwatch.Frequency;
            Console.WriteLine("Time to JIT and run: {0:N5} seconds", sw.ElapsedTicks / freq);
            Console.WriteLine("Time to JIT:         {0:N5} seconds", compileTimeTicks / freq);
            Console.WriteLine("Time to run:         {0:N5} seconds", (sw.ElapsedTicks - compileTimeTicks) / freq);
        }

        private static void RunInterpreter(OpValue[] prog)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int[] b = new int[30000];
            int next = 0;
            int idx = 0;
            while (next < prog.Length)
            {
                OpValue op = prog[next];
                switch (op.OpCode)
                {
                    case OpCode.ShiftRight:
                        idx += op.Data;
                        next++;
                        break;

                    case OpCode.ShiftLeft:
                        idx -= op.Data;
                        next++;
                        break;

                    case OpCode.Increment:
                        b[idx] += op.Data;
                        next++;
                        break;

                    case OpCode.Decrement:
                        b[idx] -= op.Data;
                        next++;
                        break;

                    case OpCode.Output:
                        char oCh = (char)b[idx];
                        Console.Write(oCh);

                        next++;
                        break;

                    case OpCode.Input:
                        b[idx] = (char)Console.Read();
                        next++;
                        break;

                    case OpCode.CondLeft:
                        if (b[idx] == 0)
                        {
                            next = op.Data;
                        }

                        next++;
                        break;

                    case OpCode.CondRight:
                        next = op.Data;
                        break;

                    case OpCode.Assign:
                        b[idx] = op.Data;
                        next++;
                        break;

                    default:
                        next++;
                        break;
                }
            }

            sw.Stop();
            Console.WriteLine("Time to interpret: {0:N5} seconds", sw.ElapsedTicks / (double)Stopwatch.Frequency);
        }
    }
}
