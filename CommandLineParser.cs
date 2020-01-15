using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    class CommandLineParser
    {
        Dictionary<string, string> mKeyValuePairs = new Dictionary<string, string>();
        public void Parse(string[] args)
        {
            var sb = new StringBuilder(1204);
            foreach (var arg in args)
            {
                sb.Append(arg);
                sb.Append(" ");
            }

            int state = 0;
            string name = string.Empty;
            string value = string.Empty;
            char valueStart = '\0';
            foreach (var ch in sb.ToString())
            {
                switch(ch)
                {
                    case '/':
                    case '-':
                        {
                            if (state == 0)
                            {
                                state = 1;
                            }
                            else if(state == 3)
                            {
                                if (ch != valueStart)
                                    value += ch;
                                else
                                {
                                    mKeyValuePairs[name] = value;
                                    name = string.Empty;
                                    value = string.Empty;
                                    state = 1;
                                }
                            }
                            else
                            {
                                mKeyValuePairs[name] = value;
                                name = string.Empty;
                                value = string.Empty;
                                state = 1;
                            }
                        }
                        break;
                    case ':':
                    case '=':
                    case ' ':
                        {
                            if (state == 1)
                                state = 2;
                            else if (state == 3)
                            {
                                if (ch != valueStart)
                                    value += ch;
                                else
                                {
                                    mKeyValuePairs[name] = value;
                                    name = string.Empty;
                                    value = string.Empty;
                                    state = 0;
                                }
                            }
                        }
                        break;
                    default:
                        {
                            if(state==1)
                            {
                                name += ch;
                            }
                            else if(state == 2)
                            {
                                if (ch == '\'' || ch == '"')
                                {
                                    state = 3;
                                    valueStart = ch;
                                }
                                else
                                {
                                    state = 3;
                                    valueStart = ' ';
                                    value += ch;
                                }
                            }
                            else if (state == 3)
                            {
                                if (ch != valueStart)
                                    value += ch;
                                else
                                {
                                    mKeyValuePairs[name] = value;
                                    name = string.Empty;
                                    value = string.Empty;
                                    state = 0;
                                }
                            }
                        }
                        break;
                }
            }
            if (!string.IsNullOrEmpty(name))
            {
                mKeyValuePairs[name] = value;
                name = string.Empty;
                value = string.Empty;
            }
        }

        public bool Has(string name)
        {
            return mKeyValuePairs.ContainsKey(name);
        }

        public int GetValue(string name, int defaultValue)
        {
            string value;
            if (!mKeyValuePairs.TryGetValue(name, out value))
                return defaultValue;
            int result;
            if (int.TryParse(value, out result))
                return result;
            return defaultValue;
        }

        public uint GetValue(string name, uint defaultValue)
        {
            string value;
            if (!mKeyValuePairs.TryGetValue(name, out value))
                return defaultValue;
            uint result;
            if (uint.TryParse(value, out result))
                return result;
            return defaultValue;
        }
        public long GetValue(string name, long defaultValue)
        {
            string value;
            if (!mKeyValuePairs.TryGetValue(name, out value))
                return defaultValue;
            long result;
            if (long.TryParse(value, out result))
                return result;
            return defaultValue;
        }

        public ulong GetValue(string name, ulong defaultValue)
        {
            string value;
            if (!mKeyValuePairs.TryGetValue(name, out value))
                return defaultValue;
            ulong result;
            if (ulong.TryParse(value, out result))
                return result;
            return defaultValue;
        }

        public float GetValue(string name, float defaultValue)
        {
            string value;
            if (!mKeyValuePairs.TryGetValue(name, out value))
                return defaultValue;
            float result;
            if (float.TryParse(value, out result))
                return result;
            return defaultValue;
        }

        public double GetValue(string name, double defaultValue)
        {
            string value;
            if (!mKeyValuePairs.TryGetValue(name, out value))
                return defaultValue;
            double result;
            if (double.TryParse(value, out result))
                return result;
            return defaultValue;
        }

        public string GetValue(string name, string defaultValue)
        {
            string value;
            if (!mKeyValuePairs.TryGetValue(name, out value))
                return defaultValue;
            return value;
        }
    }
}
