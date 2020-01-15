using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    partial class Compiler : CompilerBase
    {
        public enum OutputType
        {
            ASM,
            BIN,
            EXE
        }
        class FunctionScope
        {
            public FunctionScope(string name)
            {
                Name = name;
                Variables = new List<string>();
            }
            public string Name { get; private set; }
            public List<string> Variables { get; private set; }
        }

        class LoopBlock
        {
            public LoopBlock(string beginLabel, string endLabel)
            {
                Begin = beginLabel;
                End = endLabel;
            }
            public string Begin { get; private set; }
            public string End { get; private set; }
        }

        class FunctionInfo
        {
            public string Name { get; set; }
            public int ParameterDefCount { get; set; }
            public int ParameterMinimumCount { get; set; }
            public int Index { get; set; }
        }
        private Stack<FunctionScope> mFunctionStack = new Stack<FunctionScope>();
        private Stack<LoopBlock> mLoopBlockStack = new Stack<LoopBlock>();
        private Dictionary<string, FunctionInfo> mFunctionTable = new Dictionary<string, FunctionInfo>();
        private List<IValue> mConstants = new List<IValue>();
        private List<Instruction> mInstructions = new List<Instruction>();

        public Compiler()
        {
            mConstants.Add(new BooleanValue(false));
            mConstants.Add(new BooleanValue(true));
            for(int i=0;i<10;++i)
            {
                mConstants.Add(new IntegerValue(i));
            }
        }
        private int AddConstant(IValue value)
        {
            int idx = 0;
            foreach (var v in mConstants)
            {
                if (v.XEquals(value))
                    return idx;
                ++idx;
            }

            mConstants.Add(value);
            return mConstants.Count - 1;
        }

        private void LinkFunction()
        {
            for (int i =0;i<mInstructions.Count;++i)
            {
                var it = mInstructions[i];
                if (it.ID == InstructionID.CALL)
                {
                    var call = it as InstructionCALL;
                    FunctionInfo fi;
                    if(mFunctionTable.TryGetValue(call.Name, out fi))
                    {
                        if(call.ParameterCount< fi.ParameterMinimumCount)
                            throw new Exception($"函数调用参数个数太少：{call.Name}");
                        if(call.ParameterCount>1023)
                            throw new Exception($"函数调用参数个数超出允许的最大数量(1024)：{call.Name}");

                        if (fi.ParameterDefCount > call.ParameterCount)
                        {
                            for(int j= call.ParameterCount;j< fi.ParameterDefCount; ++j)
                            {
                                InsertLC(0,i++);
                            }
                        }
                        else if (fi.ParameterDefCount < call.ParameterCount)
                        {
                            if (fi.ParameterDefCount >= 0)
                                throw new Exception($"函数调用参数个数超出定义数量：{call.Name}");
                            mInstructions[i] = new InstructionCALLSYS(call.Name, call.ParameterCount)
                            {
                                Index = (int)((uint)((call.ParameterCount<<22) & 0xFFC00000) | (uint)(fi.Index & 0x003FFFFF))
                            };
                        }
                    }
                    else
                    {
                        throw new Exception($"无法找到函数入口：{call.Name}");
                    }
                }
                else if(it.ID == InstructionID.RET && (i<mInstructions.Count-1) && mInstructions[i+1].ID == InstructionID.RET)
                {
                    mInstructions.RemoveAt(i--);
                }
            }
        }

        private void LinkLabel()
        {
            var jmpIndex = new Dictionary<string, List<InstructionJMP>>();
            foreach(var i in mInstructions)
            {
                if(i.ID == InstructionID.JMP
                    || i.ID == InstructionID.JMPC
                    || i.ID == InstructionID.JMPN
                    || i.ID == InstructionID.CALL)
                {
                    var jmp = i as InstructionJMP;
                    List<InstructionJMP> instructions;
                    if (jmpIndex.TryGetValue(jmp.JMPLabel, out instructions))
                    {
                        instructions.Add(jmp);
                    }
                    else
                    {
                        instructions = new List<InstructionJMP>();
                        instructions.Add(jmp);
                        jmpIndex[jmp.JMPLabel] = instructions;
                    }
                }
            }

            for (int i = 0; i < mInstructions.Count; ++i)
            {
                var it = mInstructions[i];
                if (it.ID == InstructionID.NOOP)
                {
                    if (string.IsNullOrEmpty(it.Label))
                        continue;
                    if (i < mInstructions.Count - 1)
                    {
                        var next = mInstructions[i + 1];
                        if (string.IsNullOrEmpty(next.Label))
                        {
                            next.Label = it.Label;
                            mInstructions.RemoveAt(i--);
                        }
                        else 
                        {
                            List<InstructionJMP> instructions;
                            if (jmpIndex.TryGetValue(it.Label,out instructions))
                            {
                                jmpIndex.Remove(it.Label);
                                foreach (var ii in instructions)
                                {
                                    ii.JMPLabel = next.Label;
                                }
                                List<InstructionJMP> oi;
                                if (jmpIndex.TryGetValue(next.Label, out oi))
                                    oi.AddRange(instructions);
                                else
                                    jmpIndex[next.Label] = instructions;
                                mInstructions.RemoveAt(i--);
                            }
                        }
                    }
                }
            }

            int line = 0;
            foreach(var i in mInstructions)
            {
                if (!string.IsNullOrEmpty(i.Label))
                {
                    List<InstructionJMP> instructions;
                    if (jmpIndex.TryGetValue(i.Label, out instructions))
                    {
                        foreach (var ii in instructions)
                        {
                            ii.JMPIndex = line;
                        }
                    }
                }
                ++line;
            }
        }

        private void OutputASM(FileStream fs)
        {
            using (var writer = new StreamWriter(fs, Encoding.UTF8, 1024 * 8, true))
            {
                writer.WriteLine("VALUE BEGIN");
                var idx = 0;
                foreach (var v in mConstants)
                {
                    writer.WriteLine($"{idx++} : {v}");
                }
                writer.WriteLine("VALUE END");

                writer.WriteLine("PROG BEGIN");
                foreach (var i in mInstructions)
                {
                    writer.WriteLine(i.ToString());
                }
                writer.WriteLine("PROG END");
                writer.Flush();
            }
        }

        private void OutputByteCode(FileStream fs)
        {
            var fileFlag = Guid.Parse("F39FE6DA-98F6-4854-B0CB-659EF6B838CE");
            var now = DateTime.UtcNow;
            using (var writer = new BinaryWriter(fs,Encoding.UTF8, true))
            {
                var pos = fs.Position;
                writer.Write(fileFlag.ToByteArray());
                writer.Write(mConstants.Count);
                writer.Write(mInstructions.Count);
                writer.Write(default(long));
                writer.Write(default(int));
                writer.Write(default(int));
                writer.Write(default(int));
                writer.Write(default(int));
                writer.Write(now.ToBinary());
                foreach (var v in mConstants)
                {
                    writer.Write(v.ToByteArray());
                }

                foreach (var i in mInstructions)
                {
                    writer.Write(i.ToByteArray());
                }
                writer.Flush();
                writer.Seek((int)(pos + 24), SeekOrigin.Begin);
                writer.Write(writer.BaseStream.Length);
                writer.Seek(0, SeekOrigin.End);
                writer.Flush();
            }
        }

        private void OutputExecutableProg(FileStream fs, string loaderPath)
        {
            using (var loaderfs = File.OpenRead(loaderPath))
            {
                byte[] buffer = new byte[128 * 1024];
                int length = loaderfs.Read(buffer, 0, buffer.Length);
                while(length>0)
                {
                    fs.Write(buffer, 0, length);
                    length = loaderfs.Read(buffer, 0, buffer.Length);
                }
            }
            var pos = fs.Position;
            OutputByteCode(fs);
            var posbytes = BitConverter.GetBytes(fs.Position - pos);
            fs.Write(posbytes, 0, posbytes.Length);
        }

        public virtual void LoadImportDefine(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                using (var reader = new StreamReader(fs, true))
                {
                    var sp = new char[] { ' ', '\t' };
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var data = line.Split(sp, StringSplitOptions.RemoveEmptyEntries);
                        if (data.Length < 2)
                            throw new Exception($"导入定义文件格式错误：{line}");
                        string name;
                        int minp;
                        if (data.Length >= 3)
                        {
                            name = data[1].Substring(1, data[1].Length - 2);
                            minp = int.Parse(data[2]);
                        }
                        else
                        {
                            name = data[0].Substring(1, data[0].Length - 2);
                            minp = int.Parse(data[1]);
                        }
                        FunctionDefine(name, -1, minp);
                    }
                }
            }
        }
        public virtual void LinkProgram(string outpath, OutputType otype,string loaderPath)
        {
            LinkFunction();
            LinkLabel();
            using (var fs = File.OpenWrite(outpath))
            {
                fs.SetLength(0);
                if (otype== OutputType.ASM)
                    OutputASM(fs);
                else if (otype == OutputType.BIN)
                    OutputByteCode(fs);
                else if (otype == OutputType.EXE)
                    OutputExecutableProg(fs, loaderPath);
                fs.Flush();
            }
        }
        public override string CurrentLoopEndLabel
        {
            get
            {
                if (mLoopBlockStack.Count == 0)
                    throw new Exception("不能在循环外使用“跳出循环”语句");
                return mLoopBlockStack.Peek().End;
            }
        }
        public override int CurrentScopeVariableCount
        {
            get
            {
                return CurrentFunctionScope.Variables.Count;
            }
        }
        private FunctionScope CurrentFunctionScope
        {
            get
            {
                return mFunctionStack.Peek();
            }
        }

        public override void InsertADD()
        {
            mInstructions.Add(new InstructionADD());
        }

        public override void InsertAND()
        {
            mInstructions.Add(new InstructionAND());
        }

        public override IAllocDSTK InsertALLOCDSTK()
        {
            var adstk = new InstructionALLOCDSTK();
            mInstructions.Add(adstk);
            return adstk;
        }

        public override void InsertARRAYMAKE()
        {
            mInstructions.Add(new InstructionARRAYMAKE());
        }

        public override void InsertARRAYREAD(string name)
        {
            var fc = CurrentFunctionScope;
            var idx = fc.Variables.IndexOf(name);
            if (idx < 0)
                throw new Exception($"未找到变量 [{name}]");
            mInstructions.Add(new InstructionARRAYREAD(name, idx));
        }

        public override void InsertARRAYWRITE(string name)
        {
            var fc = CurrentFunctionScope;
            var idx = fc.Variables.IndexOf(name);
            if (idx < 0)
                throw new Exception($"未找到变量 [{name}]");
            mInstructions.Add(new InstructionARRAYWRITE(name,idx));
        }

        public override void InsertCALL(string name,int pc)
        {
            mInstructions.Add(new InstructionCALL(name,pc));
        }

        public override void InsertDIV()
        {
            mInstructions.Add(new InstructionDIV());
        }

        public override void EnterFunction(string name)
        {
            mFunctionStack.Push(new FunctionScope(name));
        }

        public override void EnterLoop(string beginLabel, string endLabel)
        {
            mLoopBlockStack.Push(new LoopBlock(beginLabel, endLabel));
        }

        public override void InsertEQ()
        {
            mInstructions.Add(new InstructionEQ());
        }

        public override void ExitFunction()
        {
            mFunctionStack.Pop();
        }

        public override void ExitLoop()
        {
            mLoopBlockStack.Pop();
        }

        public override void FunctionDefine(string name, int parDefCount, int parMinCount)
        {
            if (mFunctionTable.ContainsKey(name))
            {
                var fi = mFunctionTable[name];
                mFunctionTable[name] = new FunctionInfo()
                {
                    Name = name,
                    ParameterDefCount = parDefCount,
                    ParameterMinimumCount = parMinCount,
                    Index = fi.Index
                };
            }
            else
            {
                mFunctionTable[name] = new FunctionInfo()
                {
                    Name = name,
                    ParameterDefCount = parDefCount,
                    ParameterMinimumCount = parMinCount,
                    Index = mFunctionTable.Count
                };
            }
        }

        public override void InsertGT()
        {
            mInstructions.Add(new InstructionGT());
        }

        public override void InsertJMP(string jmpLabel)
        {
            mInstructions.Add(new InstructionJMP(jmpLabel));
        }

        public override void InsertJMPC(string jmpLabel)
        {
            mInstructions.Add(new InstructionJMPC(jmpLabel));
        }

        public override void InsertJMPN(string jmpLabel)
        {
            mInstructions.Add(new InstructionJMPN(jmpLabel));
        }

        public override void InsertLabel(string label)
        {
            mInstructions.Add(new InstructionNOOP()
            {
                Label = label
            });
        }

        public override void InsertLT()
        {
            mInstructions.Add(new InstructionLT());
        }

        public override void InsertLC(IValue value)
        {
            mInstructions.Add(new InstructionLC(AddConstant(value)));
        }

        public void InsertLC(int dindex,int after)
        {
            mInstructions.Insert(after,new InstructionLC(dindex));
        }

        public override void InsertMOD()
        {
            mInstructions.Add(new InstructionMOD());
        }

        public override void InsertMUL()
        {
            mInstructions.Add(new InstructionMUL());
        }

        public override void InsertNE()
        {
            mInstructions.Add(new InstructionNE());
        }

        public override void InsertNOT()
        {
            mInstructions.Add(new InstructionNOT());
        }

        public override void InsertOR()
        {
            mInstructions.Add(new InstructionOR());
        }

        public override void InsertPOP()
        {
            mInstructions.Add(new InstructionPOP());
        }

        public override void InsertPUSH()
        {
            mInstructions.Add(new InstructionPUSH());
        }

        public override void InsertSUB()
        {
            mInstructions.Add(new InstructionSUB());
        }

        public override void VariableDefine(string name)
        {
            var fc = mFunctionStack.Peek();
            fc.Variables.Add(name);
        }

        public override void InsertLD(string name)
        {
            var fc = CurrentFunctionScope;
            var idx = fc.Variables.IndexOf(name);
            if (idx < 0)
                throw new Exception($"未找到变量 [{name}]");
            mInstructions.Add(new InstructionLD(idx));
        }

        public override void InsertSD(string name)
        {
            var fc = CurrentFunctionScope;
            var idx = fc.Variables.IndexOf(name);
            if (idx < 0)
                throw new Exception($"未找到变量 [{name}]");
            mInstructions.Add(new InstructionSD(idx));
        }

        public override void InsertRET()
        {
            mInstructions.Add(new InstructionRET());
        }
    }
}
