using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    class DemoActuator : ActuatorBase
    {
        public Dictionary<string, IValue> GVariableTable { get; set; }
        public DemoActuator()
        {
            GVariableTable = new Dictionary<string, IValue>();

            FunctionTable["输出"] = WriteOutput;
            FunctionTable["输入"] = ReadInput;
            FunctionTable["转为数字"] = ValueToNumber;
            FunctionTable["转为整数字"] = ValueToInteger;
            FunctionTable["转为一句话"] = ValueToString;
            FunctionTable["向下取整"] = ValueFloor;
            FunctionTable["向上取整"] = ValueCeiling;
            FunctionTable["取阵列的行数"] = GetArrayRow;
            FunctionTable["取阵列的列数"] = GetArrayCol;
            FunctionTable["随机数"] = GetRandom;
            FunctionTable["设置窗口标题"] = SetConsoleTitle;
            FunctionTable["设置窗口背景色"] = SetConsoleBackgroundColor;
            FunctionTable["设置窗口前景色"] = SetConsoleForegroundColor;
            FunctionTable["设置窗口光标位置"] = SetConsoleCursorPosition;
            FunctionTable["读取按键值"] = ReadInputKey;
            FunctionTable["获取全局变量"] = ReadGVar;
            FunctionTable["设置全局变量"] = WriteGVar;
            FunctionTable["取当前时间"] = ReadTimeMS;
            FunctionTable["换行符"] = GetNewLine;
        }

        private IValue WriteOutput(IValue[] vargs)
        {
            foreach(var v in vargs)
            {
                Console.Write(v.AsString());
            }
            return new BooleanValue(false);
        }

        private IValue ReadInput(IValue[] vargs)
        {
            foreach (var v in vargs)
            {
                Console.Write(v.AsString());
            }
            return new StringValue(Console.ReadLine());
        }

        private IValue ValueToNumber(IValue[] vargs)
        {
            if (vargs.Length > 0)
                return new RealValue(vargs[0].AsReal());
            return new IntegerValue(0);
        }

        private IValue ValueToInteger(IValue[] vargs)
        {
            if (vargs.Length > 0)
                return new IntegerValue(vargs[0].AsInteger());
            return new IntegerValue(0);
        }

        private IValue ValueToString(IValue[] vargs)
        {
            if (vargs.Length > 0)
                return new StringValue(vargs[0].AsString());
            return new StringValue(string.Empty);
        }

        private IValue ValueFloor(IValue[] vargs)
        {
            if (vargs.Length > 0)
                return new IntegerValue((long)Math.Floor(vargs[0].AsReal()));
            return new IntegerValue(0);
        }

        private IValue ValueCeiling(IValue[] vargs)
        {
            if (vargs.Length > 0)
                return new IntegerValue((long)Math.Ceiling(vargs[0].AsReal()));
            return new IntegerValue(0);
        }

        private IValue GetArrayRow(IValue[] vargs)
        {
            if (vargs.Length > 0)
            {
                var arr = vargs[0] as ArrayValue;
                if(arr!=null)
                    return new IntegerValue(arr.Row);
            }
            return new IntegerValue(0);
        }

        private IValue GetArrayCol(IValue[] vargs)
        {
            if (vargs.Length > 0)
            {
                var arr = vargs[0] as ArrayValue;
                if (arr != null)
                    return new IntegerValue(arr.Col);
            }
            return new IntegerValue(0);
        }


        private IValue SetConsoleTitle(IValue[] vargs)
        {
            if (vargs.Length > 0)
                Console.Title = vargs[0].AsString();
            return new StringValue(Console.Title);
        }

        private IValue SetConsoleBackgroundColor(IValue[] vargs)
        {
            if (vargs.Length > 0)
            {
                ConsoleColor color;
                if (Enum.TryParse<ConsoleColor>(vargs[0].AsString(), out color))
                    Console.BackgroundColor = color;
            }
            return new StringValue(Console.BackgroundColor.ToString());
        }

        private IValue SetConsoleForegroundColor(IValue[] vargs)
        {
            if (vargs.Length > 0)
            {
                ConsoleColor color;
                if (Enum.TryParse<ConsoleColor>(vargs[0].AsString(), out color))
                    Console.ForegroundColor = color;
            }
            return new StringValue(Console.ForegroundColor.ToString());
        }

        private IValue SetConsoleCursorPosition(IValue[] vargs)
        {
            if (vargs.Length == 2)
            {
                Console.SetCursorPosition((int)vargs[0].AsReal(), (int)vargs[1].AsReal());
            }
            return new BooleanValue(true);
        }

        private IValue GetNewLine(IValue[] vargs)
        {
            return new StringValue("\r\n");
        }

        private IValue ReadInputKey(IValue[] vargs)
        {
            string key = "None";
            while (Console.KeyAvailable)
            {
                var ki = Console.ReadKey(true);
                key = ki.Key.ToString();
            }
            return new StringValue(key);
        }

        private IValue ReadGVar(IValue[] vargs)
        {
            IValue value;
            if (vargs.Length >= 1 && GVariableTable.TryGetValue(vargs[0].AsString(), out value))
                return value;
            return new BooleanValue(false);
        }
        private IValue WriteGVar(IValue[] vargs)
        {
            if (vargs.Length >= 2)
            {
                GVariableTable[vargs[0].AsString()] = vargs[1];
                return vargs[1];
            }
            return new BooleanValue(false);
        }

        private IValue ReadTimeMS(IValue[] vargs)
        {
            var time = DateTime.UtcNow - (new DateTime(1970, 1, 1, 0, 0, 0, 0));
            return new IntegerValue((long)time.TotalMilliseconds);
        }

        
        private Random mRandom = new Random();
        private IValue GetRandom(IValue[] vargs)
        {
            if (vargs.Length == 0)
                return new RealValue(mRandom.NextDouble());
            else if (vargs.Length == 1)
            {
                if(vargs[0].Is(ValueType.Integer))
                    return new IntegerValue((long)(mRandom.NextDouble()* vargs[0].AsInteger()));
                return new RealValue(mRandom.NextDouble() * vargs[0].AsReal());
            }
            else if (vargs.Length >= 2)
            {
                if (vargs[0].Is(ValueType.Integer) && vargs[1].Is(ValueType.Integer))
                    return new IntegerValue((long)(mRandom.Next((int)vargs[0].AsInteger(), (int)vargs[1].AsInteger())));
                return new RealValue((mRandom.NextDouble() * vargs[1].AsReal() + vargs[0].AsReal()) % vargs[1].AsReal());
            }
            return new RealValue(mRandom.NextDouble());
        }
        
    }
}
