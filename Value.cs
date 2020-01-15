using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cnpl
{
    enum ValueType: byte
    {
        Integer = 1,
        Real,
        String,
        Boolean,
        Array
    }

    interface IValue
    {
        bool Is(ValueType type);
        ValueType VType { get; }

        long AsInteger();

        double AsReal();
        string AsString();

        bool AsBoolean();

        IValue VEquals(IValue v2);
        IValue VNEquals(IValue v2);

        bool XEquals(IValue value);
        byte[] ToByteArray();
    }

    class IntegerValue : IValue
    {
        private long mValue;

        public bool Is(ValueType type)
        {
            return VType == type;
        }
        public ValueType VType { get=> ValueType.Integer; }

        public override string ToString()
        {
            return mValue.ToString();
        }
        public IntegerValue(long value)
        {
            mValue = value;
        }

        public bool AsBoolean()
        {
            return (mValue != default(long));
        }

        public long AsInteger()
        {
            return mValue;
        }

        public double AsReal()
        {
            return mValue;
        }

        public string AsString()
        {
            return mValue.ToString();
        }

        public IValue VEquals(IValue v2)
        {
            return new BooleanValue(mValue == v2.AsReal());
        }

        public IValue VNEquals(IValue v2)
        {
            return new BooleanValue(mValue != v2.AsReal());
        }

        public bool XEquals(IValue value)
        {
            if (ReferenceEquals(this, value))
                return true;
            if (this.GetType() != value.GetType())
                return false;
            return mValue == value.AsReal();
        }

        public byte[] ToByteArray()
        {
            var v = Utils.IntTo7BitEncode ((ulong)mValue);
            var bytes = new byte[2 + v.Length];
            bytes[0] = (byte)VType;
            bytes[1] = 0;
            Buffer.BlockCopy(v, 0, bytes, 2, v.Length);
            return bytes;
        }
    }

    class RealValue : IValue
    {
        private double mValue;

        public bool Is(ValueType type)
        {
            return VType == type;
        }

        public ValueType VType { get => ValueType.Real; }


        public override string ToString()
        {
            return mValue.ToString();
        }
        public RealValue(double value)
        {
            mValue = value;
        }

        public bool AsBoolean()
        {
            return (mValue != default(double)) ;
        }

        public long AsInteger()
        {
            return (long)mValue;
        }

        public double AsReal()
        {
            return mValue;
        }

        public string AsString()
        {
            return mValue.ToString();
        }

        public IValue VEquals(IValue v2)
        {
            return new BooleanValue(mValue == v2.AsReal());
        }

        public IValue VNEquals(IValue v2)
        {
            return new BooleanValue(mValue != v2.AsReal());
        }

        public bool XEquals(IValue value)
        {
            if (ReferenceEquals(this, value))
                return true;
            if (this.GetType() != value.GetType())
                return false;
            return mValue == value.AsReal();
        }

        public byte[] ToByteArray()
        {
            var bytes = new byte[10];
            bytes[0] = (byte)VType;
            bytes[1] = 0;
            var v = BitConverter.GetBytes(mValue);
            Buffer.BlockCopy(v, 0, bytes, 2, v.Length);
            return bytes;
        }
    }

    class StringValue : IValue
    {
        private string mValue = string.Empty;
        public StringValue(string value)
        {
            mValue = value;
        }

        public bool Is(ValueType type)
        {
            return VType == type;
        }

        public ValueType VType { get => ValueType.String; }


        public override string ToString()
        {
            return mValue;
        }
        public long AsInteger()
        {
            long r = 0;
            if (long.TryParse(mValue, out r))
                return r;
            return default(long);
        }

        public double AsReal()
        {
            double r = 0;
            if (double.TryParse(mValue, out r))
                return r;
            return default(double);
        }

        public string AsString()
        {
            return mValue;
        }

        public bool AsBoolean()
        {
            return (!string.IsNullOrEmpty(mValue));
        }
        
        public IValue VEquals(IValue v2)
        {
            return new BooleanValue(mValue == v2.AsString());
        }

        public IValue VNEquals(IValue v2)
        {
            return new BooleanValue(mValue != v2.AsString());
        }

        public bool XEquals(IValue value)
        {
            if (ReferenceEquals(this, value))
                return true;
            if (this.GetType() != value.GetType())
                return false;
            return mValue == value.AsString();
        }

        public byte[] ToByteArray()
        {
            var v = Encoding.UTF8.GetBytes(mValue);
            var l = Utils.IntTo7BitEncode((ulong)v.Length);
            var bytes = new byte[v.Length+l.Length+2];
            bytes[0] = (byte)VType;
            bytes[1] = 0;

            Buffer.BlockCopy(l, 0, bytes, 2, l.Length);
            Buffer.BlockCopy(v, 0, bytes, l.Length + 2, v.Length);
            return bytes;
        }
    }

    class BooleanValue : IValue
    {
        private bool mValue = false;
        public BooleanValue(bool value)
        {
            mValue = value;
        }
        public bool Is(ValueType type)
        {
            return VType == type;
        }

        public ValueType VType { get => ValueType.Boolean; }

        public override string ToString()
        {
            return mValue.ToString();
        }

        public long AsInteger()
        {
            if (mValue)
                return 1;
            return 0;
        }

        public double AsReal()
        {
            if (mValue)
                return 1.0;
            return 0.0;
        }

        public string AsString()
        {
            return mValue.ToString();
        }

        public bool AsBoolean()
        {
            return mValue;
        }

        public IValue VEquals(IValue v2)
        {
            return new BooleanValue(mValue == v2.AsBoolean());
        }

        public IValue VNEquals(IValue v2)
        {
            return new BooleanValue(mValue != v2.AsBoolean());
        }

        public bool XEquals(IValue value)
        {
            if (ReferenceEquals(this, value))
                return true;
            if (this.GetType() != value.GetType())
                return false;
            return mValue == value.AsBoolean();
        }

        public byte[] ToByteArray()
        {
            var bytes = new byte[4];
            bytes[0] = (byte)VType;
            bytes[1] = 0;
            if (mValue)
            {
                bytes[2] = 0x00;
                bytes[3] = 0xFF;
            }
            else
            {
                bytes[2] = 0xFF;
                bytes[3] = 0x00;
            }
            return bytes;
        }
    }

    class ArrayValue : IValue
    {
        private IValue[,] mValue = null;

        public ArrayValue(int row,int col,IValue fillValue=null)
        {
            if(ReferenceEquals(fillValue,null))
                fillValue = new BooleanValue(false);
            mValue = new IValue[row, col];
            for(var r=0;r<row;++r)
            {
                for(var c=0;c<col;++c)
                {
                    mValue[r, c] = fillValue;
                }
            }
        }

        public bool Is(ValueType type)
        {
            return VType == type;
        }

        public ValueType VType { get => ValueType.Array; }

        public override string ToString()
        {
            return AsString();
        }

        public long AsInteger()
        {
            return 0;
        }

        public double AsReal()
        {
            return 0.0;
        }

        public string AsString()
        {
            return $"[{mValue.GetLength(0)},{mValue.GetLength(1)}]";
        }

        public bool AsBoolean()
        {
            return true;
        }

        public IValue VEquals(IValue v2)
        {
            
            return new BooleanValue(ReferenceEquals(v2, this));
        }

        public IValue VNEquals(IValue v2)
        {
            return new BooleanValue(!ReferenceEquals(v2, this));
        }

        public IValue GetValue(int row, int col)
        {
            return mValue[row, col];
        }

        public void SetValue(int row, int col, IValue value)
        {
            mValue[row, col] = value;
        }

        public int Row { get { return mValue.GetLength(0); } }
        public int Col { get { return mValue.GetLength(1); } }

        public bool XEquals(IValue value)
        {
            return ReferenceEquals(this, value);
        }

        public byte[] ToByteArray()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add((byte)VType);
            bytes.Add(0);
            bytes.AddRange(Utils.IntTo7BitEncode((ulong)mValue.GetLength(0)));
            bytes.AddRange(Utils.IntTo7BitEncode((ulong)mValue.GetLength(1)));
            for(int r=0;r< mValue.GetLength(0);++r)
            {
                for (int c = 0; c < mValue.GetLength(1); ++c)
                {
                    bytes.AddRange(GetValue(r, c).ToByteArray());
                }
            }
            return bytes.ToArray();
        }
    }
}
