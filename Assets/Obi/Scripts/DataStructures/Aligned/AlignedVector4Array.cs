using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text; 
using UnityEngine;

namespace Obi
{
	public class AlignedVector4Array : IDisposable
    {
        private byte[] buffer;
        private GCHandle bufferHandle;

        private IntPtr bufferPointer;
        private readonly int length;
 
        public AlignedVector4Array(int length)
        {
			int byteAlignment = 16;
            this.length = length;

			unsafe
			{
            	buffer = new byte[length * sizeof(Vector4) + byteAlignment];
			}

            bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            long ptr = bufferHandle.AddrOfPinnedObject().ToInt64();

            // round up ptr to nearest 'byteAlignment' boundary
            ptr = (ptr + byteAlignment - 1) & ~(byteAlignment - 1);
            bufferPointer = new IntPtr(ptr);
        }
 
        ~AlignedVector4Array()
        {
            Dispose(false);
        }
 
        protected void Dispose(bool disposing)
        {
            if(bufferHandle.IsAllocated)
            {
                bufferHandle.Free();

                buffer = null;
            }
        }
 
        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
 
        public Vector4 this[int index]
        {
            get
            {
                unsafe
                {
                    return GetPointer()[index];
                }
            }
            set
            {
                unsafe
                {
                    GetPointer()[index] = value;
                }
            }
        }
 
        public int Length
        {
            get
            {

                return length;
            }
        }
 
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');

            for(int t=0;t<length;t++)
            {
                sb.Append(this[t].ToString());

                if (t < (length - 1)) sb.Append(',');

            }
            sb.Append(']');
            return sb.ToString();

        }

		public IntPtr GetIntPtr(){
			return bufferPointer;
		}
 
        public unsafe Vector4* GetPointer(int index)
        {
            return GetPointer() + index;
        }

        public unsafe Vector4* GetPointer()
        {
            return ((Vector4*) bufferPointer.ToPointer());
        }
    }
}

