using System;

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
        Assign
    }

    public struct OpValue : IEquatable<OpValue>
    {
        private OpCode opCode;
        private int data;

        public OpValue(OpCode opCode, int data)
        {
            this.opCode = opCode;
            this.data = data;
        }

        public OpCode OpCode
        {
            get { return this.opCode; }
            set { this.opCode = value; }
        }

        public int Data
        {
            get { return this.data; }
            set { this.data = value; }
        }

        public override int GetHashCode()
        {
            return this.data + ((int)this.opCode << 27);
        }

        public override bool Equals(object obj)
        {
            return obj is OpValue &&
                this.Equals((OpValue)obj);
        }

        public bool Equals(OpValue other)
        {
            return this.opCode == other.opCode &&
                this.data == other.data;
        }

        public override string ToString()
        {
            return this.opCode + " " + this.data;
        }
    }

    public sealed class Op
    {
        private OpValue opValue;

        public Op(OpCode opCode, int data)
        {
            this.opValue = new OpValue(opCode, data);
        }

        public OpCode OpCode 
        {
            get { return this.opValue.OpCode; }
        }

        public int Data
        {
            get { return this.opValue.Data; }
            set { this.opValue.Data = value; }
        }

        public OpValue OpValue
        {
            get { return this.opValue; }
            set { this.opValue = value; }
        }

        public override string ToString()
        {
            return this.opValue.ToString();
        }
    }
}

