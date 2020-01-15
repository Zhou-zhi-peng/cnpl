using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    class Lexer : IDisposable
    {
        private SourceInputStream mInputStream = null;
        private Queue<KeyValuePair<Token, string>> mBuffer = new Queue<KeyValuePair<Token, string>>();
        private const string cn_number_table = "零一二三四五六七八九十百千万亿点";
        public Lexer(SourceInputStream inputStream)
        {
            mInputStream = inputStream;
        }
        public void Dispose()
        {
            if (mInputStream != null)
                mInputStream.Dispose();
        }

        public int Line
        {
            get { return mInputStream.Line; }
        }

        public int Column
        {
            get { return mInputStream.Column; }
        }

        public Token InputToken(out string text)
        {
            if(mBuffer.Count>0)
            {
                var b = mBuffer.Dequeue();
                text = b.Value;
                return b.Key;
            }

            text = string.Empty;
            while (true)
            {
                var ch = mInputStream.Input();
                if (ch < 0)
                    break;
                if ((text.Length <= 0) && (char.IsWhiteSpace((char)ch) || ch == '\r' || ch == '\n' || ch == ' ' || ch == '\t'))
                {
                    continue;
                }

                text += (char)ch;
                if (text == "有一个数字")
                    return Token.有一个数字;
                if (text == "有一个数字")
                    return Token.有一个数字;
                else if (text == "有一句话")
                    return Token.有一句话;
                else if (text == "有一个逻辑量")
                    return Token.有一个逻辑量;
                else if (text == "有一个阵列")
                    return Token.有一个阵列;
                else if (text == "有一种方法")
                    return Token.有一种方法;
                else if (text == "接受输入")
                    return Token.接受输入;
                else if (text == "取名为")
                    return Token.取名为;
                else if (text == "行")
                    return Token.行;
                else if (text == "列")
                    return Token.列;
                else if (text == ";" || text == "；")
                    return Token.行结束符号;
                else if (text == "." || text == "。")
                    return Token.段结束符号;
                else if (text == ":" || text == "：")
                    return Token.冒号;
                else if (text == "\\" || text == "、")
                    return Token.顿号;
                else if (text == "," || text == "，")
                    return Token.辅助标点;
                else if (text == "下列操作")
                    return Token.下列操作;
                else if (text == "执行")
                    return Token.执行;
                else if (text == "次")
                    return Token.次;
                else if (text == "使用计数器")
                    return Token.使用计数器;
                else if (text == "当")
                    return Token.当;
                else if (text == "如果")
                    return Token.如果;
                else if (text == "则")
                    return Token.则;
                else if (text == "否则")
                    return Token.否则;
                else if (text == "跳出循环")
                    return Token.跳出循环;
                else if (text == "返回")
                    return Token.返回;
                else if (text == "相加")
                    return Token.相加;
                else if (text == "相减")
                    return Token.相减;
                else if (text == "相乘")
                    return Token.相乘;
                else if (text == "相除")
                    return Token.相除;
                else if (text == "取余数")
                    return Token.取余数;
                else if (text == "求余")
                    return Token.取余数;
                else if (text == "等于")
                    return Token.等于;
                else if (text == "不等于")
                    return Token.不等于;
                else if (text == "大于")
                    return Token.大于;
                else if (text == "小于")
                    return Token.小于;
                else if (text == "或者")
                    return Token.或者;
                else if (text == "并且")
                    return Token.并且;
                else if (text == "而且")
                    return Token.并且;
                else if (text == "设")
                    return Token.设;
                else if (text == "的值为")
                    return Token.的值为;
                else if (text == "的结果")
                    return Token.的结果;
                else if (text == "的第")
                    return Token.的第;
                else if (text.Length == 1 && ((ch >= '0' && ch <= '9') || ch == '+' || ch == '-'))
                {
                    GetENumber(ref text);
                    return Token.V数字值;
                }
                else if (text.Length == 1 && (cn_number_table.IndexOf((char)ch) >= 0 || ch == '正' || ch == '负'))
                {
                    GetCNumber(ref text);
                    return Token.V数字值;
                }
                else if (text.Length == 1 && (ch == '\'' || ch == '‘' || ch == '"' || ch == '“'))
                {
                    GetString(ch, ref text);
                    return Token.V字符串值;
                }
                else if (text.Length == 1 && (ch == '[' || ch == '【'))
                {
                    GetName(ch, ref text);
                    if (text == "[真]" || text == "【真】" || text == "[是]" || text == "【是】")
                        return Token.真;
                    else if (text == "[假]" || text == "【假】" || text == "[否]" || text == "【否】")
                        return Token.假;
                    return Token.V名称符号;
                }
                else if (text.Length == 1 && (ch == '<' || ch == '《'))
                {
                    GetName(ch, ref text);
                    return Token.方法调用;
                }
                else if (text.Length > 50)
                    return Token.V_ERROR;

            }
            return Token.V程序结束;
        }

        public void ReturnToken(Token token,string text)
        {
            mBuffer.Enqueue(new KeyValuePair<Token, string>(token, text));
        }
        private void GetENumber(ref string text)
        {
            bool point = false;
            while (true)
            {
                var ch = mInputStream.Input();
                if (ch >= '0' && ch <= '9')
                {
                    text += (char)ch;
                }
                else if(ch=='.' && (!point))
                {
                    point = true;
                    text += (char)ch;
                }
                else
                {
                    mInputStream.Return(ch);
                    break;
                }
            }
        }

        private void GetCNumber(ref string text)
        {
            bool point = false;
            while (true)
            {
                var ch = mInputStream.Input();
                if (cn_number_table.IndexOf((char)ch) >= 0)
                {
                    if (ch == '点')
                    {
                        if (!point)
                        {
                            point = true;
                            text += (char)ch;
                        }
                        else
                        {
                            mInputStream.Return(ch);
                            break;
                        }
                    }
                    else
                        text += (char)ch;
                }
                else
                {
                    mInputStream.Return(ch);
                    break;
                }
            }
        }

        private void GetString(int start, ref string text)
        {
            while (true)
            {
                var ch = mInputStream.Input();
                text += (char)ch;
                if ((start == '\'' || start == '"') && start == ch)
                    break;
                else if (start == '‘' && ch=='’')
                    break;
                else if (start == '“' && ch == '”')
                    break;
            }
        }
        private void GetName(int start, ref string text)
        {
            while (true)
            {
                var ch = mInputStream.Input();
                if (ch < 0)
                {
                    mInputStream.Return(ch);
                    break;
                }
                text += (char)ch;
                if (start == '[' && (ch == ']' || ch == '】'))
                    break;
                else if (start == '【' && (ch == ']' || ch == '】'))
                    break;
                else if (start == '<' && (ch == '>' || ch == '》'))
                    break;
                else if (start == '《' && (ch == '>' || ch == '》'))
                    break;
            }
        }

    }
}
