using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MTConnectAgentSimulator
{
    public class StringStream : Stream
    {
        private StringBuilder strBuilder;

        public StringStream()
        {
            strBuilder = new StringBuilder();
        }

        public StringStream(string str)
        {
            strBuilder = new StringBuilder(str);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {

        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {
                ;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int howMuchRead = 0;
            for (int i = offset; i < count; i++)
            {
                var actualIndex = i * 2;
                howMuchRead = i + 1;
                if (actualIndex >= strBuilder.Length)
                {
                    howMuchRead = count;
                    break;
                }
                string s = strBuilder[actualIndex].ToString() + strBuilder[actualIndex + 1].ToString();
                buffer[i] = Convert.ToByte(s, 16);
            }
            return howMuchRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            for (int i = offset; i < count; i++)
            {
                strBuilder.Append(buffer[i].ToString("x2"));
            }
        }
        public override string ToString()
        {
            return strBuilder.ToString();
        }
    }

}
