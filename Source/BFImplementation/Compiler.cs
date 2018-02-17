using System.Collections.Generic;

namespace BFImplementation
{
    public sealed class Compiler
    {
        private readonly Dictionary<char, OpCode> validChars = new Dictionary<char, OpCode>
        {
            { ',', OpCode.Input },
            { '.', OpCode.Output },
            { '<', OpCode.ShiftLeft },
            { '>', OpCode.ShiftRight },
            { '+', OpCode.Increment },
            { '-', OpCode.Decrement },
            { '[', OpCode.CondLeft },
            { ']', OpCode.CondRight },
        };

        public OpValue[] Compile(string program)
        {
            List<Op> opList = new List<Op>();
            for (int idx = 0; idx < program.Length; idx++)
            {
                if (!this.validChars.TryGetValue(program[idx], out var opCode))
                {
                    continue;
                }

                opList.Add(new Op(opCode, 1));
            }

            this.Optimize(opList);

            OpValue[] result = new OpValue[opList.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = opList[i].OpValue;
            }

            Stack<int> leftBraces = new Stack<int>();
            for (int curr = 0; curr < result.Length; curr++)
            {
                if (result[curr].OpCode == OpCode.CondLeft)
                {
                    leftBraces.Push(curr);
                }
                else if (result[curr].OpCode == OpCode.CondRight)
                {
                    int leftIndex = leftBraces.Pop();
                    result[leftIndex].Data = curr;
                    result[curr].Data = leftIndex;
                }
            }

            return result;
        }

        private void Optimize(List<Op> input)
        {
            // Condense null-assignments
            CondenseNullAssignment(input);

            // Condense runs
            CondenseRun(input, OpCode.Increment, OpCode.Decrement);
            CondenseRun(input, OpCode.Decrement, OpCode.Increment);
            CondenseRun(input, OpCode.ShiftRight, OpCode.ShiftLeft);
            CondenseRun(input, OpCode.ShiftLeft, OpCode.ShiftRight);
        }

        private void CondenseRun(List<Op> input, OpCode opCodeForward, OpCode opCodeBackward)
        {
            Op curr = null;
            int i = 0;
            while (i < input.Count)
            {
                if (curr == null)
                {
                    curr = input[i++];
                    if (i == input.Count ||
                        curr.OpCode != opCodeForward)
                    {
                        curr = null;
                        continue;
                    }
                }

                Op next = input[i++];
                if (next.OpCode == opCodeForward)
                {
                    curr.Data += next.Data;
                    next.Data = 0;
                }
                else if (next.OpCode == opCodeBackward)
                {
                    curr.Data -= next.Data;
                    next.Data = 0;
                }
                else
                {
                    curr = null;
                }
            }

            input.RemoveAll(op => (op.OpCode == opCodeForward || op.OpCode == opCodeBackward) && op.Data == 0);
        }

        // TODO: this is actually a special-case of a copy loop,
        // which in turn is a special case of a multiply loop.
        // So let's optimize those and delete this instead.
        private void CondenseNullAssignment(List<Op> input)
        {
            if (input.Count < 3)
            {
                return;
            }

            Op none = new Op(OpCode.None, 0);

            Op prev2 = input[0];
            Op prev = input[1];
            for (int i = 2; i < input.Count; i++)
            {
                Op curr = input[i];

                if (prev2.OpCode != OpCode.CondLeft ||
                    (prev.OpCode != OpCode.Decrement &&
                     prev.OpCode != OpCode.Increment)||
                    curr.OpCode != OpCode.CondRight)
                {
                    prev2 = prev;
                    prev = curr;
                    continue;
                }

                prev2 = input[i - 2] = new Op(OpCode.Assign, 0);
                prev = input[i - 1] = none;
                curr = input[i] = none;
            }

            input.RemoveAll(op => op == none);
        }
    }
}
