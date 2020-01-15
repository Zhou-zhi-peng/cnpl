using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    class GrammarParser
    {
        public IAST ParseProgram(Lexer lexer)
        {
            var program = new AST.Program();
            while (true)
            {
                program.AddBlockStatement(ParseBlockStatement(lexer));
                var text = string.Empty;
                var token = lexer.InputToken(out text);
                if (token == Token.V程序结束)
                    break;
                else if (token == Token.V_ERROR)
                    throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
                else
                    lexer.ReturnToken(token, text);
            }
            return program;
        }

        private AST.BlockStatement ParseBlockStatement(Lexer lexer)
        {
            var block = new AST.BlockStatement();
            var exitLoop = false;
            while (!exitLoop)
            {
                var text = string.Empty;
                var token = lexer.InputToken(out text);
                AST.Statement steatement = null;
                switch (token)
                {
                    case Token.有一个数字:
                        steatement = ParseNumberVariableStatement(lexer);
                        break;
                    case Token.有一句话:
                        steatement = ParseStringVariableStatement(lexer);
                        break;
                    case Token.有一个逻辑量:
                        steatement = ParseBooleanVariableStatement(lexer);
                        break;
                    case Token.有一个阵列:
                        steatement = ParseArrayVariableStatement(lexer);
                        break;
                    case Token.有一种方法:
                        steatement = ParseFunctionStatement(lexer);
                        break;
                    case Token.设:
                        steatement = ParseSetVariableStatement(lexer);
                        break;
                    case Token.如果:
                        steatement = ParseIfStatement(lexer);
                        break;
                    case Token.当:
                        steatement = ParseWhileStatement(lexer);
                        break;
                    case Token.下列操作:
                        steatement = ParseLoopCountStatement(lexer);
                        break;
                    case Token.跳出循环:
                        steatement = ParseBreakStatement(lexer);
                        break;
                    case Token.返回:
                        steatement = ParseReturnStatement(lexer);
                        break;
                    case Token.方法调用:
                        steatement = ParseCallExpression(lexer, ParseName(text), true);
                        break;
                    case Token.行结束符号:
                        continue;
                    case Token.段结束符号:
                        exitLoop = true;
                        continue;
                    case Token.V程序结束:
                        lexer.ReturnToken(token, text);
                        exitLoop = true;
                        continue;
                    default:
                        throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
                }
                block.AddStatement(steatement);
            }
            return block;
        }

        private AST.Statement ParseNumberVariableStatement(Lexer lexer)
        {
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token != Token.冒号)
                lexer.ReturnToken(token, text);
            var value = ParseExpression(lexer);
            
            token = lexer.InputToken(out text);
            if (token == Token.辅助标点)
                token = lexer.InputToken(out text);

            if (token != Token.取名为)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            token = lexer.InputToken(out text);
            if (token != Token.V名称符号)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            var name = ParseName(text);

            return new AST.VariableStatement(name, value);
        }

        private AST.Statement ParseBooleanVariableStatement(Lexer lexer)
        {
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token != Token.冒号)
                lexer.ReturnToken(token, text);
            var value = ParseExpression(lexer);

            token = lexer.InputToken(out text);
            if (token == Token.辅助标点)
                token = lexer.InputToken(out text);

            if (token != Token.取名为)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            token = lexer.InputToken(out text);
            if (token != Token.V名称符号)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            var name = ParseName(text);

            return new AST.VariableStatement(name, value);
        }

        private AST.Statement ParseStringVariableStatement(Lexer lexer)
        {
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token != Token.冒号)
                lexer.ReturnToken(token, text);
            var value = ParseExpression(lexer);

            token = lexer.InputToken(out text);
            if (token == Token.辅助标点)
                token = lexer.InputToken(out text);

            if (token != Token.取名为)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            token = lexer.InputToken(out text);
            if (token != Token.V名称符号)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
            var name = ParseName(text);

            return new AST.VariableStatement(name, value);
        }

        private AST.Statement ParseArrayVariableStatement(Lexer lexer)
        {
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token != Token.冒号)
                lexer.ReturnToken(token, text);

            var row = ParseExpression(lexer);

            token = lexer.InputToken(out text);
            if (token != Token.行)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            var col = ParseExpression(lexer);

            token = lexer.InputToken(out text);
            if (token != Token.列)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            token = lexer.InputToken(out text);
            if (token == Token.辅助标点)
                token = lexer.InputToken(out text);

            if (token != Token.取名为)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            token = lexer.InputToken(out text);
            if (token != Token.V名称符号)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
            var name = ParseName(text);

            AST.Expression expression;
            if(row is AST.ConstantExpression && col is AST.ConstantExpression)
            {
                var r = (int)((row as AST.ConstantExpression).Value.AsReal());
                var c = (int)((col as AST.ConstantExpression).Value.AsReal());
                expression = new AST.ConstantExpression(new ArrayValue(r, c));
            }
            else
            {
                expression = new AST.MakeArrayExpression(row, col, new BooleanValue(false));
            }
            return new AST.VariableStatement(name, expression);
        }

        private AST.Statement ParseFunctionStatement(Lexer lexer)
        {
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token == Token.冒号)
                token = lexer.InputToken(out text);
            var parameters = new List<string>();
            if (token != Token.取名为)
            {
                if (token != Token.接受输入)
                    throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

                token = lexer.InputToken(out text);
                if (token == Token.冒号)
                    token = lexer.InputToken(out text);

                while (true)
                {
                    if (token != Token.V名称符号)
                        throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
                    parameters.Add(ParseName(text));
                    token = lexer.InputToken(out text);
                    if (token == Token.顿号)
                        token = lexer.InputToken(out text);
                    else
                        break;
                }

                if (token == Token.辅助标点)
                    token = lexer.InputToken(out text);
            }
            if (token != Token.取名为)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            token = lexer.InputToken(out text);
            if (token != Token.V名称符号)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
            var name = ParseName(text);

            token = lexer.InputToken(out text);
            if (token != Token.冒号)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            var block = ParseBlockStatement(lexer);

            return new AST.FunctionStatement(name, parameters.ToArray(), block);
        }
        
        private AST.Statement ParseSetVariableStatement(Lexer lexer)
        {
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token != Token.V名称符号)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
            var name = ParseName(text);

            AST.Expression row = null;
            AST.Expression col = null;
            token = lexer.InputToken(out text);
            if (token == Token.的第)
            {
                row = ParseExpression(lexer);
                token = lexer.InputToken(out text);
                if (token != Token.行)
                    throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

                col = ParseExpression(lexer);
                token = lexer.InputToken(out text);
                if (token != Token.列)
                    throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
                token = lexer.InputToken(out text);
            }

            if (token != Token.的值为)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            token = lexer.InputToken(out text);
            if (token != Token.冒号)
                lexer.ReturnToken(token, text);

            var expr = ParseExpression(lexer);

            token = lexer.InputToken(out text);
            if (token != Token.的结果)
                lexer.ReturnToken(token, text);
            if(row!=null|| col!=null)
                return new AST.SetArrayVariableStatement(name, row, col, expr);
            return new AST.SetVariableStatement(name, expr);
        }

        private AST.Statement ParseIfStatement(Lexer lexer)
        {
            var expr = ParseExpression(lexer);
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token == Token.辅助标点)
                token = lexer.InputToken(out text);

            if (token != Token.则)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            token = lexer.InputToken(out text);
            if (token != Token.冒号)
                lexer.ReturnToken(token, text);

            var trueBlock = ParseBlockStatement(lexer);
            AST.Statement falseBlock = null;
            token = lexer.InputToken(out text);
            if (token == Token.否则)
            {
                token = lexer.InputToken(out text);
                if (token != Token.冒号)
                    lexer.ReturnToken(token, text);
                falseBlock = ParseBlockStatement(lexer);
            }
            else
                lexer.ReturnToken(token, text);

            return new AST.IfStatement(expr, trueBlock, falseBlock);
        }

        private AST.Statement ParseWhileStatement(Lexer lexer)
        {
            var expr = ParseExpression(lexer);

            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token == Token.辅助标点)
                token = lexer.InputToken(out text);


            if (token != Token.执行)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            token = lexer.InputToken(out text);
            if (token != Token.下列操作)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
            
            token = lexer.InputToken(out text);
            if (token != Token.冒号)
                lexer.ReturnToken(token, text);

            var loopBlock = ParseBlockStatement(lexer);
            return new AST.WhileStatement(expr, loopBlock);
        }

        private AST.Statement ParseLoopCountStatement(Lexer lexer)
        {
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token != Token.执行)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            var expr = ParseExpression(lexer);

            token = lexer.InputToken(out text);
            if (token != Token.次)
                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

            var indexName = string.Empty;
            token = lexer.InputToken(out text);
            if (token == Token.辅助标点)
            {
                token = lexer.InputToken(out text);
                if (token == Token.使用计数器)
                {
                    token = lexer.InputToken(out text);
                    if (token != Token.V名称符号)
                        throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
                    indexName = ParseName(text);
                }
                else
                    throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
            }
            else
            {
                if (token == Token.使用计数器)
                {
                    token = lexer.InputToken(out text);
                    if (token != Token.V名称符号)
                        throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
                    indexName = ParseName(text);
                }
                else
                    lexer.ReturnToken(token, text);
            }
            token = lexer.InputToken(out text);
            if (token != Token.冒号)
                lexer.ReturnToken(token, text);

            var loopBlock = ParseBlockStatement(lexer);
            return new AST.LoopCountStatement(expr, loopBlock, indexName);
        }

        private AST.Statement ParseBreakStatement(Lexer lexer)
        {
            return new AST.BreakStatement();
        }

        private AST.Statement ParseReturnStatement(Lexer lexer)
        {
            AST.Expression expr = null;
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            lexer.ReturnToken(token, text);

            if (token != Token.行结束符号 && token != Token.段结束符号 && token != Token.V程序结束)
                expr = ParseExpression(lexer);
            return new AST.ReturnStatement(expr);
        }

        private AST.Expression ParseExpression(Lexer lexer)
        {
            var lexpr = ParseExpressionL0(lexer);
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            switch (token)
            {
                case Token.或者:
                    {
                        var rexpr = ParseExpression(lexer);
                        return new AST.ORExpression(lexpr, rexpr);
                    }
                case Token.并且:
                    {
                        var rexpr = ParseExpression(lexer);
                        return new AST.ANDExpression(lexpr, rexpr);
                    }
                case Token.顿号:
                    {
                        var rexpr = ParseExpression(lexer);
                        token = lexer.InputToken(out text);
                        AST.Expression expr = null;
                        switch (token)
                        {
                            case Token.相加:
                                expr = new AST.AddExpression(lexpr, rexpr);
                                break;
                            case Token.相减:
                                expr = new AST.SubExpression(lexpr, rexpr);
                                break;
                            case Token.相乘:
                                expr = new AST.MulExpression(lexpr, rexpr);
                                break;
                            case Token.相除:
                                expr = new AST.DivExpression(lexpr, rexpr);
                                break;
                            case Token.取余数:
                                expr = new AST.ModExpression(lexpr, rexpr);
                                break;
                            default:
                                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
                        }
                        token = lexer.InputToken(out text);
                        if (token != Token.的结果)
                            lexer.ReturnToken(token, text);
                        return expr;
                    }
                default:
                    lexer.ReturnToken(token, text);
                    break;
            }
            return lexpr;
        }
        private AST.Expression ParseExpressionL0(Lexer lexer)
        {
            var lexpr = ParseExpressionL1(lexer);
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            switch (token)
            {
                case Token.等于:
                    {
                        var rexpr = ParseExpressionL1(lexer);
                        return new AST.EQExpression(lexpr, rexpr);
                    }
                case Token.不等于:
                    {
                        var rexpr = ParseExpressionL1(lexer);
                        return new AST.NEQExpression(lexpr, rexpr);
                    }
                case Token.大于:
                    {
                        var rexpr = ParseExpressionL1(lexer);
                        return new AST.GTExpression(lexpr, rexpr);
                    }
                case Token.小于:
                    {
                        var rexpr = ParseExpressionL1(lexer);
                        return new AST.LTExpression(lexpr, rexpr);
                    }
                case Token.顿号:
                    {
                        var rexpr = ParseExpression(lexer);
                        token = lexer.InputToken(out text);
                        AST.Expression expr = null;
                        switch (token)
                        {
                            case Token.相加:
                                expr = new AST.AddExpression(lexpr, rexpr);
                                break;
                            case Token.相减:
                                expr = new AST.SubExpression(lexpr, rexpr);
                                break;
                            case Token.相乘:
                                expr = new AST.MulExpression(lexpr, rexpr);
                                break;
                            case Token.相除:
                                expr = new AST.DivExpression(lexpr, rexpr);
                                break;
                            case Token.取余数:
                                expr = new AST.ModExpression(lexpr, rexpr);
                                break;
                            default:
                                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
                        }
                        token = lexer.InputToken(out text);
                        if (token != Token.的结果)
                            lexer.ReturnToken(token, text);
                        return expr;
                    }
                default:
                    lexer.ReturnToken(token, text);
                    break;
            }
            return lexpr;
        }

        private AST.Expression ParseExpressionL1(Lexer lexer)
        {
            var lexpr = ParseExpressionL2(lexer);
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            switch (token)
            {
                case Token.顿号:
                    {
                        var rexpr = ParseExpression(lexer);
                        token = lexer.InputToken(out text);
                        AST.Expression expr = null;
                        switch (token)
                        {
                            case Token.相加:
                                expr = new AST.AddExpression(lexpr, rexpr);
                                break;
                            case Token.相减:
                                expr = new AST.SubExpression(lexpr, rexpr);
                                break;
                            case Token.相乘:
                                expr = new AST.MulExpression(lexpr, rexpr);
                                break;
                            case Token.相除:
                                expr = new AST.DivExpression(lexpr, rexpr);
                                break;
                            case Token.取余数:
                                expr = new AST.ModExpression(lexpr, rexpr);
                                break;
                            default:
                                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
                        }
                        token = lexer.InputToken(out text);
                        if (token != Token.的结果)
                            lexer.ReturnToken(token, text);
                        return expr;
                    }
                default:
                    lexer.ReturnToken(token, text);
                    break;
            }
            return lexpr;
        }

        private AST.Expression ParseExpressionL2(Lexer lexer)
        {
            AST.Expression expr = null;
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            switch (token)
            {
                case Token.V名称符号:
                    {
                        var name = ParseName(text);
                        token = lexer.InputToken(out text);
                        if (token == Token.的第)
                        {
                            var row = ParseExpression(lexer);
                            token = lexer.InputToken(out text);
                            if (token != Token.行)
                                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

                            var col = ParseExpression(lexer);
                            token = lexer.InputToken(out text);
                            if (token != Token.列)
                                throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");

                            expr = new AST.ReadArrayVariableExpression(name, row, col);
                        }
                        else
                        {
                            lexer.ReturnToken(token, text);
                            expr = new AST.ReadVariableExpression(name);
                        }
                    }
                    break;
                case Token.V字符串值:
                    {
                        var svalue = ParseStringValue(text);
                        expr = new AST.ConstantExpression(new StringValue(svalue));
                    }
                    break;
                case Token.V数字值:
                    {
                        var dvalue = ParseNumberValue(text);
                        if (((long)dvalue) == dvalue)
                            expr = new AST.ConstantExpression(new IntegerValue((long)dvalue));
                        else
                            expr = new AST.ConstantExpression(new RealValue(dvalue));
                    }
                    break;
                case Token.真:
                    {
                        expr = new AST.ConstantExpression(new BooleanValue(true));
                    }
                    break;
                case Token.假:
                    {
                        expr = new AST.ConstantExpression(new BooleanValue(false));
                    }
                    break;
                case Token.方法调用:
                    expr = ParseCallExpression(lexer, ParseName(text), false);
                    break;
                default:
                    throw new Exception($"语法错误，不是预期的输入:{text}。 在 {lexer.Line} 行，{lexer.Column}列");
            }
            return expr;
        }

        AST.Expression ParseCallExpression(Lexer lexer, string name, bool bStatement)
        {
            var parameters = new List<AST.Expression>();
            var text = string.Empty;
            var token = lexer.InputToken(out text);
            if (token != Token.冒号)
            {
                lexer.ReturnToken(token, text);
            }
            else
            {
                while (true)
                {
                    var p = ParseExpression(lexer);
                    parameters.Add(p);
                    text = string.Empty;
                    token = lexer.InputToken(out text);
                    if (token != Token.辅助标点)
                    {
                        lexer.ReturnToken(token, text);
                        break;
                    }
                }
            }
            if(bStatement)
                return new AST.CallStatement(name, parameters.ToArray());
            return new AST.CallExpression(name, parameters.ToArray());
        }

        static string mCNumbers = "零一二三四五六七八九";
        static string[] mCUnits = new string[] { "十", "百", "千","万", "十万", "百万", "千万", "亿", "十亿", "百亿", "千亿", "万亿", "十万亿", "百万亿", "千万亿", "万万亿" };
        private double ParseNumberValue(string text)
        {
            
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            if (char.IsDigit(text[0]) || text[0]=='+' || text[0]=='-')
                return double.Parse(text);

            var arr = text.Split(new char[] { '点' }, 2);
            long ivalue = 0;
            long ifv = 1;
            var sivalue = arr[0];
            //整数部分
            for (int i= sivalue.Length-1;i>=0;--i)
            {
                var v = sivalue[i];
                var idx = mCNumbers.IndexOf(v);
                if(idx>=0)
                {
                    ivalue += idx * ifv;
                }
                else
                {
                    var unit = string.Empty;
                    do
                    {
                        unit = unit.Insert(0,char.ToString(v));
                        if (i == 0)
                            break;
                        v = sivalue[--i];
                    } while (mCNumbers.IndexOf(v) < 0);
                    ++i;
                    idx = Array.IndexOf(mCUnits,unit);
                    if(idx>=0)
                    {
                        ifv = (long)Math.Pow(10, idx + 1);
                    }
                }
            }
            
            if(arr.Length>1)
            {
                var sfvalue = arr[1];
                var ffv = 0.1;
                var fvalue = 0.0;
                for (int i=0;i< sfvalue.Length;++i)
                {
                    var idx = mCNumbers.IndexOf(sfvalue[i]);
                    if (idx >= 0)
                    {
                        fvalue += idx * ffv;
                        ffv = ffv / 10.0;
                    }
                }
                return ivalue + fvalue;
            }
            return (double)ivalue;
        }

        private string ParseName(string text)
        {
            return text.Substring(1, text.Length - 2);
        }

        private string ParseStringValue(string text)
        {
            return text.Substring(1, text.Length - 2);
        }
    }
}
