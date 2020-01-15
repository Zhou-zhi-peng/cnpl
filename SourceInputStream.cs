using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    class SourceInputStream : IDisposable
    {
        private TextReader mIStream = null;
        private Queue<int> mBuffer = new Queue<int>();
        public SourceInputStream(string path)
        {
            mIStream = new StreamReader(File.OpenRead(path));
            Line = 0;
            Column = 0;
        }

        public int Line { get; private set; }
        public int Column { get; private set; }

        public void Dispose()
        {
            if (mIStream != null)
                mIStream.Close();
        }

        public int Input()
        {
            if (mBuffer.Count > 0)
                return mBuffer.Dequeue();
            var ch = mIStream.Read();
            if (ch == '\n')
            {
                Line++;
                Column = 0;
            }
            else
            {
                Column++;
            }
            return ch;
        }

        public void Return(int ch)
        {
            mBuffer.Enqueue(ch);
        }
    }
}
