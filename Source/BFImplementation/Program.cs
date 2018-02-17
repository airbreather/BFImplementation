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
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: 2 arguments, which should be the path to a BF source file and then the path to the input (both UTF-8 encoded).");
                return 1;
            }

            string path = args[0];
            string progText = File.ReadAllText(args[0], Encoding.UTF8);
            string inputText = File.ReadAllText(args[1], Encoding.UTF8);

            OpValue[] prog = new Compiler().Compile(progText);

            var oldOut = Console.Out;
            var oldIn = Console.In;
            foreach (var act in new Action<OpValue[]>[] { RunJIT, RunInterpreter, RunCompiled })
            {
                var sb = new StringBuilder();
                using (var rd = new StringReader(inputText))
                using (var wr = new StringWriter(sb))
                {
                    try
                    {
                        Console.SetIn(rd);
                        Console.SetOut(wr);
                        act(prog);
                    }
                    finally
                    {
                        Console.SetOut(oldOut);
                        Console.SetIn(oldIn);
                    }
                }

                Console.WriteLine(sb);
                Console.WriteLine();
            }

            return 0;
        }

        private static void RunCompiled(OpValue[] prog)
        {
            /*
            Stopwatch sw = Stopwatch.StartNew();
            string exe = new NativeCreator().Create(prog);
            long compileTimeTicks = sw.ElapsedTicks;

            using (var process = Process.Start(exe))
            {
                process.WaitForExit();
                sw.Stop();
            }

            Console.WriteLine("Compiled to " + exe);

            double freq = Stopwatch.Frequency;
            Console.WriteLine("Time to compile and run: {0:N5} seconds", sw.ElapsedTicks / freq);
            Console.WriteLine("Time to compile:         {0:N5} seconds", compileTimeTicks / freq);
            Console.WriteLine("Time to run:             {0:N5} seconds", (sw.ElapsedTicks - compileTimeTicks) / freq);
            */
        }

        private static unsafe void RunJIT(OpValue[] prog)
        {
            int[] data = new int[300000];
            fixed (int* p = data)
            {
                object[] args = { new IntPtr(p) };
                Stopwatch sw = Stopwatch.StartNew();
                System.Reflection.MethodInfo compiled = new AssemblyCreator().Create(prog);

                long compileTimeTicks = sw.ElapsedTicks;

                compiled.Invoke(null, args);
                sw.Stop();

                double freq = Stopwatch.Frequency;
                Console.WriteLine("Time to JIT and run: {0:N5} seconds", sw.ElapsedTicks / freq);
                Console.WriteLine("Time to JIT:         {0:N5} seconds", compileTimeTicks / freq);
                Console.WriteLine("Time to run:         {0:N5} seconds", (sw.ElapsedTicks - compileTimeTicks) / freq);
            }
        }

        private static unsafe void RunInterpreter(OpValue[] prog)
        {
            int[] arr = new int[30000];
            fixed (int* pnnd = arr)
            {
                Stopwatch sw = Stopwatch.StartNew();
                int next = 0;
                int* b = pnnd;
                while (next < prog.Length)
                {
                    OpValue op = prog[next];
                    switch (op.OpCode)
                    {
                        case OpCode.ShiftRight:
                            b += op.Data;
                            break;

                        case OpCode.ShiftLeft:
                            b -= op.Data;
                            break;

                        case OpCode.Increment:
                            *b += op.Data;
                            break;

                        case OpCode.Decrement:
                            *b -= op.Data;
                            break;

                        case OpCode.Output:
                            char oCh = (char)*b;
                            Console.Write(oCh);
                            break;

                        case OpCode.Input:
                            *b = Console.Read();
                            break;

                        case OpCode.CondLeft:
                            if (*b == 0)
                            {
                                next = op.Data;
                            }

                            break;

                        case OpCode.CondRight:
                            next = op.Data - 1;
                            break;

                        case OpCode.Assign:
                            *b = op.Data;
                            break;
                    }

                    next++;
                }

                sw.Stop();
                Console.WriteLine("Time to interpret: {0:N5} seconds", sw.ElapsedTicks / (double)Stopwatch.Frequency);
            }
        }
    }
}
