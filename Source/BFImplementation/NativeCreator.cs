using System.Diagnostics;
using System.IO;

namespace BFImplementation
{
    public class NativeCreator
    {
        private const string Pre = @"
#include <stdlib.h>
#include <stdio.h>

int main()
{
    char* arr = (char*)malloc(300000);
    char* ptr = arr;
";

        private const string Post = @"
    free(arr);
    return 0;
}
";

        public string Create(OpValue[] prog)
        {
            string ePath = Path.GetTempFileName();
            ProcessStartInfo info = new ProcessStartInfo("gcc", "-O2 -x c -o " + ePath + " -")
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(info))
            {
                process.StandardInput.WriteLine(Pre);

                foreach (OpValue op in prog)
                {
                    this.Append(process.StandardInput, op);
                }

                process.StandardInput.WriteLine(Post);

                process.StandardInput.Flush();
                process.StandardInput.Close();

                process.WaitForExit();
            }

            return ePath;
        }

        private void Append(StreamWriter sw, OpValue op)
        {
            switch (op.OpCode)
            {
                case OpCode.ShiftRight:
                    sw.WriteLine("ptr += " + op.Data + ";");
                    break;

                case OpCode.ShiftLeft:
                    sw.WriteLine("ptr -= " + op.Data + ";");
                    break;

                case OpCode.Increment:
                    sw.WriteLine("*ptr += " + op.Data + ";");
                    break;

                case OpCode.Decrement:
                    sw.WriteLine("*ptr -= " + op.Data + ";");
                    break;

                case OpCode.CondLeft:
                    sw.WriteLine("while (*ptr) {");
                    break;

                case OpCode.CondRight:
                    sw.WriteLine("}");
                    break;

                case OpCode.Output:
                    sw.WriteLine("putchar(*ptr);");
                    break;

                case OpCode.Input:
                    sw.WriteLine("*ptr = getchar();");
                    break;

                case OpCode.Assign:
                    sw.WriteLine("*ptr = " + op.Data + ";");
                    break;
            }
        }
    }
}

