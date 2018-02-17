using System;
using System.Runtime.InteropServices;

namespace BFImplementation
{
    public enum OpCode : byte
    {
        None,
        ShiftRight,
        ShiftLeft,
        Increment,
        Decrement,
        Output,
        Input,
        CondLeft,
        CondRight,
        Assign,
    }

    [StructLayout(LayoutKind.Auto)]
    public struct OpValue : IEquatable<OpValue>
    {
        public OpCode OpCode;

        public int Data;

        public OpValue(OpCode opCode, int data)
        {
            this.OpCode = opCode;
            this.Data = data;
        }

        public override int GetHashCode() => this.Data + ((int)this.OpCode << 27);

        public override bool Equals(object obj) => obj is OpValue other && this.Equals(other);

        public bool Equals(OpValue other) => this.OpCode == other.OpCode && this.Data == other.Data;

        public override string ToString() => this.OpCode + " " + this.Data;
    }

    public sealed class Op
    {
        public OpValue OpValue;

        public Op(OpCode opCode, int data) => this.OpValue = new OpValue(opCode, data);

        public OpCode OpCode => this.OpValue.OpCode;

        public int Data
        {
            get => this.OpValue.Data;
            set => this.OpValue.Data = value;
        }

        public override string ToString() => this.OpValue.ToString();
    }
}
