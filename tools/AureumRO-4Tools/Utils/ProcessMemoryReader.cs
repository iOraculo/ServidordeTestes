using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace _4RTools.Utils
{
    internal class ProcessMemoryReader : IDisposable
    {
        private static class MemoryApi
        {
            [Flags]
            public enum ProcessAccessType : uint
            {
                PROCESS_VM_READ = 0x0010,
                PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ReadProcessMemory(
                IntPtr hProcess,
                IntPtr lpBaseAddress,
                [Out] byte[] buffer,
                int size,
                out IntPtr lpNumberOfBytesRead);
        }

        private readonly object handleLock = new object();
        private Process m_ReadProcess = null;
        private IntPtr m_hProcess = IntPtr.Zero;
        private bool disposed = false;

        public Process ReadProcess
        {
            get { return this.m_ReadProcess; }
            set { this.m_ReadProcess = value; }
        }

        public bool IsOpen
        {
            get
            {
                lock (this.handleLock)
                {
                    return this.m_hProcess != IntPtr.Zero && !this.disposed;
                }
            }
        }

        public bool OpenProcess()
        {
            lock (this.handleLock)
            {
                CloseHandleInternal();

                if (this.m_ReadProcess == null)
                {
                    return false;
                }

                uint dwDesiredAccess = (uint)(MemoryApi.ProcessAccessType.PROCESS_VM_READ | MemoryApi.ProcessAccessType.PROCESS_QUERY_LIMITED_INFORMATION);
                this.m_hProcess = MemoryApi.OpenProcess(dwDesiredAccess, false, (uint)this.m_ReadProcess.Id);

                if (this.m_hProcess == IntPtr.Zero)
                {
                    Logger.Warn("ProcessMemoryReader", "OpenProcess failed for PID " + this.m_ReadProcess.Id + " (Win32 error " + Marshal.GetLastWin32Error() + ")");
                    return false;
                }

                return true;
            }
        }

        public void CloseHandle()
        {
            lock (this.handleLock)
            {
                CloseHandleInternal();
            }
        }

        private void CloseHandleInternal()
        {
            if (this.m_hProcess != IntPtr.Zero)
            {
                MemoryApi.CloseHandle(this.m_hProcess);
                this.m_hProcess = IntPtr.Zero;
            }
        }

        public bool ReadProcessMemory(IntPtr memoryAddress, byte[] buffer, int bytesToRead, out int bytesRead)
        {
            bytesRead = 0;

            if (memoryAddress == IntPtr.Zero || buffer == null || bytesToRead <= 0 || buffer.Length < bytesToRead)
            {
                return false;
            }

            IntPtr handle;
            lock (this.handleLock)
            {
                handle = this.m_hProcess;
            }

            if (handle == IntPtr.Zero)
            {
                return false;
            }

            IntPtr numberOfBytesRead;
            bool success = MemoryApi.ReadProcessMemory(handle, memoryAddress, buffer, bytesToRead, out numberOfBytesRead);
            bytesRead = numberOfBytesRead.ToInt32();
            return success && bytesRead > 0;
        }

        public byte[] ReadProcessMemory(IntPtr memoryAddress, uint bytesToRead, out int bytesRead)
        {
            byte[] array = new byte[bytesToRead];
            ReadProcessMemory(memoryAddress, array, (int)bytesToRead, out bytesRead);
            return array;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            lock (this.handleLock)
            {
                CloseHandleInternal();
                this.disposed = true;
            }
        }

        ~ProcessMemoryReader()
        {
            Dispose(false);
        }
    }
}
