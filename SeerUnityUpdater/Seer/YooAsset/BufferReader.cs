using System.Diagnostics;
using System.Text;

namespace SeerUnityUpdater.Seer.YooAsset
{
    internal class BufferReader
    {
        public BufferReader(byte[] data)
        {
            this._buffer = data;
        }

        public bool IsValid
        {
            get
            {
                return this._buffer != null && this._buffer.Length != 0;
            }
        }

        public int Capacity
        {
            get
            {
                return this._buffer.Length;
            }
        }

        public byte[] ReadBytes(int count)
        {
            byte[] array = new byte[count];
            System.Buffer.BlockCopy(this._buffer, this._index, array, 0, count);
            this._index += count;
            return array;
        }

        public byte ReadByte()
        {
            byte[] buffer = this._buffer;
            int index = this._index;
            this._index = index + 1;
            return buffer[index];
        }

        public bool ReadBool()
        {
            byte[] buffer = this._buffer;
            int index = this._index;
            this._index = index + 1;
            return buffer[index] == 1;
        }

        public short ReadInt16()
        {
            if (BitConverter.IsLittleEndian)
            {
                short result = (short)((int)this._buffer[this._index] | (int)this._buffer[this._index + 1] << 8);
                this._index += 2;
                return result;
            }
            short result2 = (short)((int)this._buffer[this._index] << 8 | (int)this._buffer[this._index + 1]);
            this._index += 2;
            return result2;
        }

        public ushort ReadUInt16()
        {
            return (ushort)this.ReadInt16();
        }

        public int ReadInt32()
        {
            if (BitConverter.IsLittleEndian)
            {
                int result = (int)this._buffer[this._index] | (int)this._buffer[this._index + 1] << 8 | (int)this._buffer[this._index + 2] << 16 | (int)this._buffer[this._index + 3] << 24;
                this._index += 4;
                return result;
            }
            int result2 = (int)this._buffer[this._index] << 24 | (int)this._buffer[this._index + 1] << 16 | (int)this._buffer[this._index + 2] << 8 | (int)this._buffer[this._index + 3];
            this._index += 4;
            return result2;
        }

        public uint ReadUInt32()
        {
            return (uint)this.ReadInt32();
        }

        public long ReadInt64()
        {
            if (BitConverter.IsLittleEndian)
            {
                ulong num = (ulong)((int)this._buffer[this._index] | (int)this._buffer[this._index + 1] << 8 | (int)this._buffer[this._index + 2] << 16 | (int)this._buffer[this._index + 3] << 24);
                int num2 = (int)this._buffer[this._index + 4] | (int)this._buffer[this._index + 5] << 8 | (int)this._buffer[this._index + 6] << 16 | (int)this._buffer[this._index + 7] << 24;
                this._index += 8;
                return (long)(num | (ulong)((ulong)((long)num2) << 32));
            }
            int num3 = (int)this._buffer[this._index] << 24 | (int)this._buffer[this._index + 1] << 16 | (int)this._buffer[this._index + 2] << 8 | (int)this._buffer[this._index + 3];
            ulong num4 = (ulong)((int)this._buffer[this._index + 4] << 24 | (int)this._buffer[this._index + 5] << 16 | (int)this._buffer[this._index + 6] << 8 | (int)this._buffer[this._index + 7]);
            this._index += 8;
            return (long)(num4 | (ulong)((ulong)((long)num3) << 32));
        }

        public ulong ReadUInt64()
        {
            return (ulong)this.ReadInt64();
        }

        public string ReadUTF8()
        {
            ushort num = this.ReadUInt16();
            if (num == 0)
            {
                return string.Empty;
            }
            string @string = Encoding.UTF8.GetString(this._buffer, this._index, (int)num);
            this._index += (int)num;
            return @string;
        }

        public int[] ReadInt32Array()
        {
            ushort num = this.ReadUInt16();
            int[] array = new int[(int)num];
            for (int i = 0; i < (int)num; i++)
            {
                array[i] = this.ReadInt32();
            }
            return array;
        }

        public long[] ReadInt64Array()
        {
            ushort num = this.ReadUInt16();
            long[] array = new long[(int)num];
            for (int i = 0; i < (int)num; i++)
            {
                array[i] = this.ReadInt64();
            }
            return array;
        }

        public string[] ReadUTF8Array()
        {
            ushort num = this.ReadUInt16();
            string[] array = new string[(int)num];
            for (int i = 0; i < (int)num; i++)
            {
                array[i] = this.ReadUTF8();
            }
            return array;
        }

        [Conditional("DEBUG")]
        private void CheckReaderIndex(int length)
        {
            if (this._index + length > this.Capacity)
            {
                throw new IndexOutOfRangeException();
            }
        }

        private readonly byte[] _buffer;

        private int _index;
    }
}
