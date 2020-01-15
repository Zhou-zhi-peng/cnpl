using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    partial class Compiler
    {
        enum InstructionID : ushort
        {
            NOOP = 0,
            ADD,
            AND,
            ALLOCDSTK,
            ARRAYMAKE,
            ARRAYREAD,
            ARRAYWRITE,
            CALL,
            CALLSYS,
            DIV,
            EQ,
            GT,
            JMP,
            JMPC,
            JMPN,
            LT,
            LC,
            LD,
            MOD,
            MUL,
            NE,
            NOT,
            OR,
            POP,
            PUSH,
            RET,
            SUB,
            SD
        }
        interface Instruction
        {
            string Label { get; set; }
            InstructionID ID { get; }

            byte[] ToByteArray();
        }

        abstract class InstructionBase : Instruction
        {
            public InstructionBase()
            {
                Label = null;
            }
            public string Label { get; set; }

            public abstract InstructionID ID { get; }

            public virtual byte[] ToByteArray()
            {
                return BitConverter.GetBytes((ushort)ID);
            }

            public override string ToString()
            {
                return $"{ID}";
            }
        }

        class InstructionADD : InstructionBase
        {
            public override InstructionID ID => InstructionID.ADD;
        }

        class InstructionAND : InstructionBase
        {
            public override InstructionID ID => InstructionID.AND;
        }

        class InstructionALLOCDSTK : InstructionBase, IAllocDSTK
        {
            public int Size { get; set; }
            public override InstructionID ID => InstructionID.ALLOCDSTK;

            public override byte[] ToByteArray()
            {
                var id = BitConverter.GetBytes((ushort)ID);
                var idx = Utils.IntTo7BitEncode((ulong)Size);
                var bytes = new byte[id.Length + idx.Length];
                Buffer.BlockCopy(id, 0, bytes, 0, id.Length);
                Buffer.BlockCopy(idx, 0, bytes, id.Length, idx.Length);
                return bytes;
            }

            public override string ToString()
            {
                return $"{base.ToString()} {Size}";
            }
        }

        
        class InstructionARRAYMAKE : InstructionBase
        {
            public override InstructionID ID => InstructionID.ARRAYMAKE;
        }

        class InstructionARRAYREAD : InstructionBase
        {
            public InstructionARRAYREAD(string name,int index)
            {
                Name = name;
                Index = index;
            }

            public string Name { get; private set; }
            public int Index { get; private set; }
            public override InstructionID ID => InstructionID.ARRAYREAD;

            public override byte[] ToByteArray()
            {
                var id = BitConverter.GetBytes((ushort)ID);
                var idx = Utils.IntTo7BitEncode((ulong)Index);
                var bytes = new byte[id.Length + idx.Length];
                Buffer.BlockCopy(id, 0, bytes, 0, id.Length);
                Buffer.BlockCopy(idx, 0, bytes, id.Length, idx.Length);
                return bytes;
            }

            public override string ToString()
            {
                return $"{base.ToString()} {Index}";
            }
        }

        class InstructionARRAYWRITE : InstructionBase
        {
            public InstructionARRAYWRITE(string name, int index)
            {
                Name = name;
                Index = index;
            }

            public string Name { get; private set; }
            public int Index { get; private set; }
            public override InstructionID ID => InstructionID.ARRAYWRITE;
            public override byte[] ToByteArray()
            {
                var id = BitConverter.GetBytes((ushort)ID);
                var idx = Utils.IntTo7BitEncode((ulong)Index);
                var bytes = new byte[id.Length + idx.Length];
                Buffer.BlockCopy(id, 0, bytes, 0, id.Length);
                Buffer.BlockCopy(idx, 0, bytes, id.Length, idx.Length);
                return bytes;
            }
            public override string ToString()
            {
                return $"{base.ToString()} {Index}";
            }
        }

        class InstructionCALL : InstructionJMP
        {
            public InstructionCALL(string name,int pc):
                base($"<{name}>")
            {
                Name = name;
                ParameterCount = pc;
            }
            public string Name { get; private set; }
            public int ParameterCount { get; private set; }
            public override InstructionID ID => InstructionID.CALL;
        }

        class InstructionCALLSYS : InstructionCALL
        {
            public InstructionCALLSYS(string name, int pc):
                base(name,pc)
            {
            }

            public int Index { get=>JMPIndex; set=>JMPIndex=value; }
            
            public override InstructionID ID => InstructionID.CALLSYS;
            public override string ToString()
            {
                return $"{ID.ToString()} {(Index & 0xFFC00000) >> 22} {Index & 0x003FFFFF}";
            }
        }

        class InstructionDIV : InstructionBase
        {
            public override InstructionID ID => InstructionID.DIV;
        }

        class InstructionEQ : InstructionBase
        {
            public override InstructionID ID => InstructionID.EQ;
        }

        class InstructionGT : InstructionBase
        {
            public override InstructionID ID => InstructionID.GT;
        }

        class InstructionJMP : InstructionBase
        {
            public InstructionJMP(string jmpLabel)
            {
                JMPLabel = jmpLabel;
            }

            public string JMPLabel { get; set; }

            public int JMPIndex { get; set; }

            public override InstructionID ID => InstructionID.JMP;

            public override string ToString()
            {
                return $"{base.ToString()} {JMPIndex}";
            }

            public override byte[] ToByteArray()
            {
                var id = BitConverter.GetBytes((ushort)ID);
                var idx = Utils.IntTo7BitEncode((ulong)JMPIndex);
                var bytes = new byte[id.Length + idx.Length];
                Buffer.BlockCopy(id, 0, bytes, 0, id.Length);
                Buffer.BlockCopy(idx, 0, bytes, id.Length, idx.Length);
                return bytes;
            }
        }

        class InstructionJMPC : InstructionJMP
        {
            public InstructionJMPC(string jmpLabel):base(jmpLabel)
            {
            }
            public override InstructionID ID => InstructionID.JMPC;
        }

        class InstructionJMPN : InstructionJMP
        {
            public InstructionJMPN(string jmpLabel) : base(jmpLabel)
            {
            }
            public override InstructionID ID => InstructionID.JMPN;
        }

        class InstructionNOOP : InstructionBase
        {
            public override InstructionID ID => InstructionID.NOOP;
        }

        class InstructionLT : InstructionBase
        {
            public override InstructionID ID => InstructionID.LT;
        }

        class InstructionLC : InstructionBase
        {
            public InstructionLC(int index)
            {
                Index = index;
            }

            public override InstructionID ID => InstructionID.LC;
            public int Index { get; private set; }

            public override string ToString()
            {
                return $"{base.ToString()} {Index}";
            }

            public override byte[] ToByteArray()
            {
                var id = BitConverter.GetBytes((ushort)ID);
                var idx = Utils.IntTo7BitEncode((ulong)Index);
                var bytes = new byte[id.Length + idx.Length];
                Buffer.BlockCopy(id, 0, bytes, 0, id.Length);
                Buffer.BlockCopy(idx, 0, bytes, id.Length, idx.Length);
                return bytes;
            }
        }

        class InstructionLD : InstructionBase
        {
            public InstructionLD(int index)
            {
                Index = index;
            }

            public override InstructionID ID => InstructionID.LD;
            public int Index { get; private set; }

            public override string ToString()
            {
                return $"{base.ToString()} {Index}";
            }

            public override byte[] ToByteArray()
            {
                var id = BitConverter.GetBytes((ushort)ID);
                var idx = Utils.IntTo7BitEncode((ulong)Index);
                var bytes = new byte[id.Length + idx.Length];
                Buffer.BlockCopy(id, 0, bytes, 0, id.Length);
                Buffer.BlockCopy(idx, 0, bytes, id.Length, idx.Length);
                return bytes;
            }
        }

        class InstructionMOD : InstructionBase
        {
            public override InstructionID ID => InstructionID.MOD;
        }

        class InstructionMUL : InstructionBase
        {
            public override InstructionID ID => InstructionID.MUL;
        }

        class InstructionNE : InstructionBase
        {
            public override InstructionID ID => InstructionID.NE;
        }

        class InstructionNOT : InstructionBase
        {
            public override InstructionID ID => InstructionID.NOT;
        }

        class InstructionOR : InstructionBase
        {
            public override InstructionID ID => InstructionID.OR;
        }

        class InstructionPOP : InstructionBase
        {
            public override InstructionID ID => InstructionID.POP;
        }

        class InstructionPUSH : InstructionBase
        {
            public override InstructionID ID => InstructionID.PUSH;
        }

        class InstructionSUB : InstructionBase
        {
            public override InstructionID ID => InstructionID.SUB;
        }

        class InstructionSD : InstructionBase
        {
            public InstructionSD(int index)
            {
                Index = index;
            }

            public override InstructionID ID => InstructionID.SD;
            public int Index { get; private set; }

            public override string ToString()
            {
                return $"{base.ToString()} {Index}";
            }

            public override byte[] ToByteArray()
            {
                var id = BitConverter.GetBytes((ushort)ID);
                var idx = Utils.IntTo7BitEncode((ulong)Index);
                var bytes = new byte[id.Length + idx.Length];
                Buffer.BlockCopy(id, 0, bytes, 0, id.Length);
                Buffer.BlockCopy(idx, 0, bytes, id.Length, idx.Length);
                return bytes;
            }
        }

        class InstructionRET : InstructionBase
        {
            public override InstructionID ID => InstructionID.RET;
        }
    }
}
