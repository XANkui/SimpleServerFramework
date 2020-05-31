using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetServer.Net
{
    public class ByteArray
    {
        // 默认大小
        public const int DEFAULT_SIZE = 1024;

        // 初始大小
        private int m_initSize = 0;

        // 缓冲区
        public byte[] Bytes;

        //读写位置(ReadIndex 开始读的索引，WriteIndex 已经写入的索引)
        public int ReadIndex = 0;
        public int WriteIndex = 0;

        // 容量
        private int Capacity = 0;

        // 剩余空间
        public int Remain { get { return Capacity - WriteIndex; } }

        // 数据长度
        public int Length { get { return WriteIndex - ReadIndex; } }


        public ByteArray() {

            Bytes = new byte[DEFAULT_SIZE];

            Capacity = DEFAULT_SIZE;
            m_initSize = DEFAULT_SIZE;
            ReadIndex = 0;
            WriteIndex = 0;
        }

        /// <summary>
        /// 检测并移动数据
        /// </summary>
        public void CheckAndMoveBytes() {
            if (Length < 8) {
                MoveBytes();
            }
        }

        /// <summary>
        /// 移动数据
        /// </summary>
        public void MoveBytes() {
            if (ReadIndex < 0) {
                return;
            }

            Array.Copy(Bytes,ReadIndex, Bytes,0,Length);
            WriteIndex = Length;
            ReadIndex = 0;
        }

        /// <summary>
        /// 数据长度超出缓存长度，重设数据长度
        /// </summary>
        /// <param name="size"></param>
        public void Resize(int size) {

            if (ReadIndex <0) return;
            if (size <Length) return;
            if (size <m_initSize) return;

            int n = 1024;
            while (n < size) n *= 2;

            Capacity = n;
            byte[] newBytes = new byte[Capacity];
            Array.Copy(Bytes, ReadIndex, newBytes,0,Length);
            Bytes = newBytes;
            WriteIndex = Length;
            ReadIndex = 0;
        }
    }
}
