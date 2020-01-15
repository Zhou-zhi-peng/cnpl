using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    interface IAST
    {
        string Title { get; }
        IValue Execute(ActuatorBase state);

        void Compile(CompilerBase state);
    }

    abstract class ActuatorBase
    {
        public ActuatorBase()
        {
            BreakLoop = false;
            BreakProgram = false;
            VariableTable = new Dictionary<string, IValue>();
            FunctionTable = new Dictionary<string, Func<IValue[], IValue>>();
        }
        public bool BreakLoop { get; set; }
        public bool BreakProgram { get; set; }
        public Dictionary<string, IValue> VariableTable { get; set; }
        public Dictionary<string, Func<IValue[], IValue>> FunctionTable { get; set; }
    }

    interface IAllocDSTK
    {
        int Size { get; set; }
    }

    abstract class CompilerBase
    {
        public abstract string CurrentLoopEndLabel { get; }
        public abstract int CurrentScopeVariableCount { get; }

        public abstract void VariableDefine(string name);
        public abstract void FunctionDefine(string name, int parDefCount, int parMinCount);
        public abstract void EnterFunction(string name);
        public abstract void ExitFunction();
        public abstract IAllocDSTK InsertALLOCDSTK();
        public abstract void InsertSD(string name);
        public abstract void InsertARRAYWRITE(string name);
        public abstract void InsertLabel(string label);
        public abstract void InsertJMPC(string jmpLabel);
        public abstract void InsertJMPN(string jmpLabel);
        public abstract void InsertJMP(string endLabel);
        public abstract void EnterLoop(string beginLabel, string endLabel);
        public abstract void ExitLoop();
        public abstract void InsertLC(IValue value);
        public abstract void InsertLD(string name);
        public abstract void InsertPOP();
        public abstract void InsertPUSH();
        public abstract void InsertADD();
        public abstract void InsertSUB();
        public abstract void InsertARRAYMAKE();
        public abstract void InsertCALL(string name,int pc);
        public abstract void InsertARRAYREAD(string name);
        public abstract void InsertMUL();
        public abstract void InsertDIV();
        public abstract void InsertMOD();
        public abstract void InsertEQ();
        public abstract void InsertNE();
        public abstract void InsertNOT();
        public abstract void InsertGT();
        public abstract void InsertLT();
        public abstract void InsertAND();
        public abstract void InsertOR();
        public abstract void InsertRET();
    }

    namespace AST
    {
        class Program : IAST
        {
            List<BlockStatement> mStatements= new List<BlockStatement>();

            public string Title => "Program";

            public IValue Execute(ActuatorBase state)
            {
                IValue value = new IntegerValue(0);
                foreach (var s in mStatements)
                {
                    value = s.Execute(state);
                    if (state.BreakProgram)
                    {
                        state.BreakProgram = false;
                        break;
                    }
                }
                return value;
            }

            public void AddBlockStatement(BlockStatement block)
            {
                mStatements.Add(block);
            }

            public void Compile(CompilerBase state)
            {
                state.EnterFunction("<Program>");
                var iadstk = state.InsertALLOCDSTK();
                foreach (var s in mStatements)
                {
                    s.Compile(state);
                }
                state.InsertLC(new BooleanValue(false));
                state.InsertRET();
                iadstk.Size = state.CurrentScopeVariableCount;
                state.ExitFunction();
            }
        }

        abstract class Statement : IAST
        {
            public abstract string Title { get; }

            public abstract void Compile(CompilerBase state);

            public abstract IValue Execute(ActuatorBase state);
        }

        class VariableStatement : Statement
        {
            public string Name { get; private set; }

            public Expression Value { get; private set; }

            public override string Title => $"变量申明:{Name}";

            public VariableStatement(string name, Expression value)
            {
                Name = name;
                Value = value;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var v = Value.Execute(state);
                state.VariableTable[Name] = v;
                return v;
            }

            public override void Compile(CompilerBase state)
            {
                state.VariableDefine(Name);
                Value.Compile(state);
                state.InsertSD(Name);
            }
        }

        class FunctionStatement : Statement
        {
            public override string Title => $"函数定义:{Name}";
            public string Name { get; private set; }
            public string[] Parameters { get; private set; }
            public BlockStatement mBlockStatement { get; private set; }
            public FunctionStatement(string name, string[] parameters, BlockStatement block)
            {
                Name = name;
                Parameters = parameters;
                mBlockStatement = block;
            }

            public override IValue Execute(ActuatorBase state)
            {
                state.FunctionTable[Name] = (IValue[] args)=> 
                {
                    var vt = state.VariableTable;
                    var bp = state.BreakProgram;
                    var newVT= new Dictionary<string, IValue>();
                    var i = 0;
                    foreach (var pn in Parameters)
                    {
                        if (i < args.Length)
                            newVT[pn] = args[i++];
                        else
                            newVT[pn] = new IntegerValue(0);
                    }
                    
                    state.VariableTable = newVT;
                    try
                    {
                        state.BreakProgram = false;
                        return mBlockStatement.Execute(state);
                    }
                    finally
                    {
                        state.VariableTable = vt;
                        state.BreakProgram = bp;
                    }
                };
                return new BooleanValue(true);
            }

            public override void Compile(CompilerBase state)
            {
                var cid = Guid.NewGuid();
                var end = $"{cid}-function-end";
                state.InsertJMP(end);
                state.FunctionDefine(Name, Parameters.Length, 0);
                state.EnterFunction(Name);
                state.InsertLabel($"<{Name}>");
                var iadstk = state.InsertALLOCDSTK();
                foreach (var p in Parameters)
                {
                    state.VariableDefine(p);
                }
                foreach (var p in Parameters)
                {
                    state.InsertSD(p);
                }

                mBlockStatement.Compile(state);
                state.InsertLC(new BooleanValue(false));
                state.InsertRET();
                iadstk.Size = state.CurrentScopeVariableCount;
                state.ExitFunction();
                state.InsertLabel(end);
            }
        }

        class SetVariableStatement : Statement
        {
            public override string Title => $"写变量:{Name}";
            public string Name { get; private set; }

            public Expression Value { get; private set; }
            public SetVariableStatement(string name, Expression value)
            {
                Name = name;
                Value = value;
            }

            public override IValue Execute(ActuatorBase state)
            {
                if(!state.VariableTable.ContainsKey(Name))
                    return new IntegerValue(0);
                var value = Value.Execute(state);
                state.VariableTable[Name] = value;
                return value;
            }

            public override void Compile(CompilerBase state)
            {
                Value.Compile(state);
                state.InsertSD(Name);
            }
        }

        class SetArrayVariableStatement : Statement
        {
            public override string Title => $"写数组变量:{Name}";
            public string Name { get; private set; }
            private Expression mRow;
            private Expression mCol;
            public Expression Value { get; private set; }
            public SetArrayVariableStatement(string name, Expression row, Expression col, Expression value)
            {
                Name = name;
                mRow = row;
                mCol = col;
                Value = value;
            }

            public override IValue Execute(ActuatorBase state)
            {
                IValue value;
                if (state.VariableTable.TryGetValue(Name,out value) && value is ArrayValue)
                {
                    var row = (int)mRow.Execute(state).AsReal();
                    var col = (int)mCol.Execute(state).AsReal();
                    var v = Value.Execute(state);
                    var arr = value as ArrayValue;
                    arr.SetValue(row, col, v);
                    return v;
                }
                return new IntegerValue(0);
            }

            public override void Compile(CompilerBase state)
            {
                mRow.Compile(state);
                mCol.Compile(state);
                Value.Compile(state);
                state.InsertARRAYWRITE(Name);
            }
        }

        class IfStatement : Statement
        {
            public override string Title => $"条件分支-如果";
            private Expression mConditional;
            private Statement mTrueBlock;
            private Statement mFalseBlock;
            public IfStatement(Expression conditional, Statement trueBlock, Statement falseBlock)
            {
                mConditional = conditional;
                mTrueBlock = trueBlock;
                mFalseBlock = falseBlock;
            }

            public override IValue Execute(ActuatorBase state)
            {
                IValue value;
                if (mConditional.Execute(state).AsBoolean())
                    value = mTrueBlock.Execute(state);
                else
                {
                    if (mFalseBlock != null)
                        value = mFalseBlock.Execute(state);
                    else
                        value = new BooleanValue(false);
                }
                return value;
            }
            public override void Compile(CompilerBase state)
            {
                var cid = Guid.NewGuid();
                var falseLabel = $"{cid}-false";
                var endLabel = $"{cid}-end";
                mConditional.Compile(state);
                if (mFalseBlock != null)
                    state.InsertJMPN(falseLabel);
                else
                    state.InsertJMPN(endLabel);
                mTrueBlock.Compile(state);
                state.InsertJMP(endLabel);
                if (mFalseBlock != null)
                {
                    state.InsertLabel(falseLabel);
                    mFalseBlock.Compile(state);
                }
                state.InsertLabel(endLabel);
            }
        }

        class WhileStatement : Statement
        {
            public override string Title => $"条件循环-当";
            private Expression mConditional;
            private Statement mLoopBlock;
            public WhileStatement(Expression conditional, Statement loopBlock)
            {
                mConditional = conditional;
                mLoopBlock = loopBlock;
            }

            public override IValue Execute(ActuatorBase state)
            {
                IValue value = mConditional.Execute(state);
                while (value.AsBoolean())
                {
                    value = mLoopBlock.Execute(state);

                    if(state.BreakLoop)
                    {
                        state.BreakLoop = false;
                        break;
                    }

                    if(state.BreakProgram)
                    {
                        break;
                    }
                    value = mConditional.Execute(state);
                }
                return value;
            }

            public override void Compile(CompilerBase state)
            {
                var cid = Guid.NewGuid();
                var beginLabel = $"{cid}-begin";
                var endLabel = $"{cid}-end";
                state.EnterLoop(beginLabel, endLabel);
                state.InsertLabel(beginLabel);
                mConditional.Compile(state);
                state.InsertJMPN(endLabel);
                mLoopBlock.Compile(state);
                state.InsertJMP(beginLabel);
                state.InsertLabel(endLabel);
                state.ExitLoop();
            }
        }

        class LoopCountStatement : Statement
        {
            public override string Title => $"次数循环-次";
            private Expression mCount;
            private Statement mLoopBlock;
            private string IndexName;
            public LoopCountStatement(Expression count, Statement loopBlock,string indexName)
            {
                mCount = count;
                mLoopBlock = loopBlock;
                IndexName = indexName;
            }
            public override IValue Execute(ActuatorBase state)
            {
                IValue value = mCount.Execute(state);
                long c = (long)value.AsReal();
                long i = 0;
                var indexName = string.IsNullOrEmpty(IndexName) ? $"<index>" : IndexName;
                state.VariableTable[indexName] = new IntegerValue(i);
                while ( i < c)
                {
                    value = mLoopBlock.Execute(state);
                    ++i;
                    c = (long)(mCount.Execute(state).AsReal());
                    if (state.BreakLoop)
                    {
                        state.BreakLoop = false;
                        break;
                    }

                    if (state.BreakProgram)
                    {
                        break;
                    }
                    state.VariableTable[indexName] = new IntegerValue(i);
                    value = mCount.Execute(state);
                    c = (long)value.AsReal();
                }
                return value;
            }

            public override void Compile(CompilerBase state)
            {
                var cid = Guid.NewGuid();
                var beginLabel = $"{cid}-begin";
                var endLabel = $"{cid}-end";
                var indexName = string.IsNullOrEmpty(IndexName)?$"<{cid}-index>": IndexName;
                state.EnterLoop(beginLabel, endLabel);
                state.VariableDefine(indexName);
                state.InsertLC(new IntegerValue(0));
                state.InsertSD(indexName);

                state.InsertLabel(beginLabel);
                state.InsertLD(indexName);
                mCount.Compile(state);
                state.InsertLT();
                state.InsertJMPN(endLabel);
                mLoopBlock.Compile(state);
                state.InsertLD(indexName);
                state.InsertLC(new IntegerValue(1));
                state.InsertADD();
                state.InsertSD(indexName);
                state.InsertJMP(beginLabel);
                state.InsertLabel(endLabel);
                state.ExitLoop();
            }
        }

        class BreakStatement : Statement
        {
            public override string Title => $"中止循环";
            public override IValue Execute(ActuatorBase state)
            {
                IValue value = new BooleanValue(false);
                state.BreakLoop = true;
                return value;
            }

            public override void Compile(CompilerBase state)
            {
                var endLabel = state.CurrentLoopEndLabel;
                state.InsertJMP(endLabel);
            }
        }

        class ReturnStatement : Statement
        {
            public override string Title => $"返回";
            private Expression mExpression;
            public ReturnStatement(Expression expression)
            {
                mExpression = expression;
            }

            public override IValue Execute(ActuatorBase state)
            {
                IValue value = null;
                if (mExpression != null)
                {
                    value = mExpression.Execute(state);
                }
                else
                {
                    value = new BooleanValue(false);
                }
                state.BreakProgram = true;
                return value;
            }

            public override void Compile(CompilerBase state)
            {
                if (mExpression != null)
                    mExpression.Compile(state);
                else
                    state.InsertLC(new BooleanValue(false));
                state.InsertRET();
            }
        }

        class BlockStatement : Statement
        {
            public override string Title => $"语句块({mStatements.Count})";
            List<Statement> mStatements = new List<Statement>();
            public override IValue Execute(ActuatorBase state)
            {
                IValue value = new IntegerValue(0);
                foreach (var s in mStatements)
                {
                    value = s.Execute(state);
                    if (state.BreakProgram)
                        break;
                    if (state.BreakLoop)
                        break;
                }
                return value;
            }

            public void AddStatement(Statement statement)
            {
                mStatements.Add(statement);
            }

            public override void Compile(CompilerBase state)
            {
                foreach (var s in mStatements)
                    s.Compile(state);
            }
        }


        abstract class Expression : Statement
        {
            public override string Title => $"表达式";
        }

        class AddExpression : Expression
        {
            public override string Title => $"加法";
            private Expression mExpression1;
            private Expression mExpression2;
            public AddExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var a = mExpression1.Execute(state);
                var b = mExpression2.Execute(state);
                if (a.Is(ValueType.String) || b.Is(ValueType.String))
                {
                    return new StringValue(a.AsString() + b.AsString());
                }
                else if (a.Is(ValueType.Real) || b.Is(ValueType.Real))
                {
                    return new RealValue(a.AsReal() + b.AsReal());
                }
                return new IntegerValue(a.AsInteger() + b.AsInteger());
            }

            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertADD();
            }
        }

        class SubExpression : Expression
        {
            public override string Title => $"减法";
            private Expression mExpression1;
            private Expression mExpression2;
            public SubExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var a = mExpression1.Execute(state);
                var b = mExpression2.Execute(state);
                if (a.Is(ValueType.String) || b.Is(ValueType.String))
                {
                    return new StringValue(a.AsString().TrimEnd() + b.AsString().TrimStart());
                }
                else if (a.Is(ValueType.Real) || b.Is(ValueType.Real))
                {
                    return new RealValue(a.AsReal() - b.AsReal());
                }
                return new IntegerValue(a.AsInteger() - b.AsInteger());
            }
            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertSUB();
            }
        }

        class MulExpression : Expression
        {
            public override string Title => $"乘法";
            private Expression mExpression1;
            private Expression mExpression2;
            public MulExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }
            public override IValue Execute(ActuatorBase state)
            {
                var a = mExpression1.Execute(state);
                var b = mExpression2.Execute(state);
                if (a.Is(ValueType.Integer) && b.Is(ValueType.Integer))
                    return new IntegerValue(a.AsInteger() * b.AsInteger());
                return new RealValue(a.AsReal() * b.AsReal());
            }

            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertMUL();
            }
        }

        class DivExpression : Expression
        {
            public override string Title => $"除法";
            private Expression mExpression1;
            private Expression mExpression2;
            public DivExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var a = mExpression1.Execute(state);
                var b = mExpression2.Execute(state);
                if (a.Is(ValueType.Integer) && b.Is(ValueType.Integer))
                    return new IntegerValue(a.AsInteger() / b.AsInteger());
                return new RealValue(a.AsReal() / b.AsReal());
            }

            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertDIV();
            }
        }

        class ModExpression : Expression
        {
            public override string Title => $"求余";
            private Expression mExpression1;
            private Expression mExpression2;
            public ModExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }
            public override IValue Execute(ActuatorBase state)
            {
                var v1 = mExpression1.Execute(state);
                var v2 = mExpression2.Execute(state);
                return new IntegerValue(v1.AsInteger() % v2.AsInteger());
            }

            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertMOD();
            }
        }

        class EQExpression : Expression
        {
            public override string Title => $"等于比较";
            private Expression mExpression1;
            private Expression mExpression2;
            public EQExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var v1 = mExpression1.Execute(state);
                var v2 = mExpression2.Execute(state);
                return v1.VEquals(v2);
            }

            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertEQ();
            }
        }

        class NEQExpression : Expression
        {
            public override string Title => $"不等于比较";
            private Expression mExpression1;
            private Expression mExpression2;
            public NEQExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var v1 = mExpression1.Execute(state);
                var v2 = mExpression2.Execute(state);
                return v1.VNEquals(v2);
            }
            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertNE();
            }
        }

        class GTExpression : Expression
        {
            public override string Title => $"大于比较";
            private Expression mExpression1;
            private Expression mExpression2;
            public GTExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var a = mExpression1.Execute(state);
                var b = mExpression2.Execute(state);
                if (a.Is(ValueType.String) || b.Is(ValueType.String))
                {
                    return new BooleanValue(a.AsString().Length > b.AsString().Length);
                }
                else if (a.Is(ValueType.Real) || b.Is(ValueType.Real))
                {
                    return new BooleanValue(a.AsReal() > b.AsReal());
                }
                return new BooleanValue(a.AsInteger() > b.AsInteger());
            }
            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertGT();
            }
        }

        class LTExpression : Expression
        {
            public override string Title => $"小于比较";
            private Expression mExpression1;
            private Expression mExpression2;
            public LTExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var a = mExpression1.Execute(state);
                var b = mExpression2.Execute(state);
                if (a.Is(ValueType.String) || b.Is(ValueType.String))
                {
                    return new BooleanValue(a.AsString().Length > b.AsString().Length);
                }
                else if (a.Is(ValueType.Real) || b.Is(ValueType.Real))
                {
                    return new BooleanValue(a.AsReal() > b.AsReal());
                }
                return new BooleanValue(a.AsInteger() < b.AsInteger());
            }

            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertLT();
            }
        }

        class ANDExpression : Expression
        {
            public override string Title => $"逻辑与";
            private Expression mExpression1;
            private Expression mExpression2;
            public ANDExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var v1 = mExpression1.Execute(state);
                var v2 = mExpression2.Execute(state);
                return new BooleanValue(v1.AsBoolean() && v2.AsBoolean());
            }

            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertAND();
            }
        }

        class ORExpression : Expression
        {
            public override string Title => $"逻辑或";
            private Expression mExpression1;
            private Expression mExpression2;
            public ORExpression(Expression expression1, Expression expression2)
            {
                mExpression1 = expression1;
                mExpression2 = expression2;
            }

            public override IValue Execute(ActuatorBase state)
            {
                var v1 = mExpression1.Execute(state);
                var v2 = mExpression2.Execute(state);
                return new BooleanValue(v1.AsBoolean() || v2.AsBoolean());
            }

            public override void Compile(CompilerBase state)
            {
                mExpression1.Compile(state);
                mExpression2.Compile(state);
                state.InsertOR();
            }
        }

        class ReadVariableExpression : Expression
        {
            public override string Title => $"读变量:{Name}";
            private string Name;
            public ReadVariableExpression(string name)
            {
                Name = name;
            }
            public override IValue Execute(ActuatorBase state)
            {
                IValue value;
                if (state.VariableTable.TryGetValue(Name, out value))
                    return value;
                return new IntegerValue(0);
            }
            public override void Compile(CompilerBase state)
            {
                state.InsertLD(Name);
            }
        }

        class ReadArrayVariableExpression : Expression
        {
            public override string Title => $"读数组变量:{Name}";
            private string Name;
            private Expression mRow;
            private Expression mCol;
            public ReadArrayVariableExpression(string name, Expression row, Expression col)
            {
                Name = name;
                mRow = row;
                mCol = col;
            }
            public override IValue Execute(ActuatorBase state)
            {
                IValue value;
                if (state.VariableTable.TryGetValue(Name, out value))
                {
                    if(value.GetType() == typeof(ArrayValue))
                    {
                        var row = (int)mRow.Execute(state).AsReal();
                        var col = (int)mCol.Execute(state).AsReal();
                        return (value as ArrayValue).GetValue(row, col);
                    }
                    return value;
                }
                return new IntegerValue(0);
            }
            public override void Compile(CompilerBase state)
            {
                mRow.Compile(state);
                mCol.Compile(state);
                state.InsertARRAYREAD(Name);
            }
        }

        class CallExpression : Expression
        {
            public override string Title => $"调用函数:{Name}";
            protected string Name;
            protected Expression[] mParameters;
            public CallExpression(string name, Expression[] parameters)
            {
                Name = name;
                mParameters = parameters;
            }
            public override IValue Execute(ActuatorBase state)
            {
                Func<IValue[],IValue> func;
                var parameters = new IValue[mParameters.Length];
                int idx = 0;
                foreach(var p in mParameters)
                {
                    parameters[idx++] = p.Execute(state);
                }

                if (state.FunctionTable.TryGetValue(Name, out func))
                    return func.Invoke(parameters);
                else
                    throw new Exception($"函数未找到:{Name}");
            }

            public override void Compile(CompilerBase state)
            {
                foreach (var p in mParameters.Reverse())
                    p.Compile(state);
                state.InsertCALL(Name, mParameters.Length);
            }
        }

        class CallStatement : CallExpression
        {
            public CallStatement(string name, Expression[] parameters):
                base(name,parameters)
            {
            }

            public override void Compile(CompilerBase state)
            {
                foreach (var p in mParameters.Reverse())
                    p.Compile(state);
                state.InsertCALL(Name, mParameters.Length);
                state.InsertPOP();
            }
        }

        class MakeArrayExpression : Expression
        {
            public override string Title => $"构造数组";
            private Expression Row;
            private Expression Col;
            private IValue Value;
            public MakeArrayExpression(Expression row, Expression col,IValue fillValue)
            {
                Row = row;
                Col = col;
                Value = fillValue;
            }
            public override IValue Execute(ActuatorBase state)
            {
                var r = (int)(Row.Execute(state).AsReal());
                var c = (int)(Col.Execute(state).AsReal());
                return new ArrayValue(r, c, Value);
            }

            public override void Compile(CompilerBase state)
            {
                Row.Compile(state);
                Col.Compile(state);
                state.InsertLC(Value);
                state.InsertARRAYMAKE();
            }
        }
        class ConstantExpression : Expression
        {
            public override string Title => $"常量:{Value}";
            public IValue Value { get; private set; }
            public ConstantExpression(IValue value)
            {
                Value = value;
            }
            public override IValue Execute(ActuatorBase state)
            {
                return Value;
            }
            public override void Compile(CompilerBase state)
            {
                state.InsertLC(Value);
            }
        }
    }
}
