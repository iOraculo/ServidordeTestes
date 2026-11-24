using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace _4RTools.Utils
{
    internal class ProcessMemoryReader : IDisposable
    {
        public struct MemoryRegion
        {
            public IntPtr BaseAddress;
            public int RegionSize;
        }

        private static class MemoryApi
        {
            [Flags]
            public enum ProcessAccessType : uint
            {
                PROCESS_VM_READ = 0x0010,
                PROCESS_QUERY_INFORMATION = 0x0400,
                PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
            }

            public const uint MEM_COMMIT = 0x1000;
            public const uint PAGE_READWRITE = 0x04;
            public const uint PAGE_WRITECOPY = 0x08;
            public const uint PAGE_EXECUTE_READWRITE = 0x40;
            public const uint PAGE_GUARD = 0x100;
            public const uint PAGE_NOACCESS = 0x01;

            [StructLayout(LayoutKind.Sequential)]
            public struct MEMORY_BASIC_INFORMATION
            {
                public IntPtr BaseAddress;
                public IntPtr AllocationBase;
                public uint AllocationProtect;
                public IntPtr RegionSize;
                public uint State;
                public uint Protect;
                public uint Type;
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

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr VirtualQueryEx(
                IntPtr hProcess,
                IntPtr lpAddress,
                out MEMORY_BASIC_INFORMATION lpBuffer,
                IntPtr dwLength);
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

                uint fullQueryAccess = (uint)(MemoryApi.ProcessAccessType.PROCESS_VM_READ | MemoryApi.ProcessAccessType.PROCESS_QUERY_INFORMATION);
                this.m_hProcess = MemoryApi.OpenProcess(fullQueryAccess, false, (uint)this.m_ReadProcess.Id);

                if (this.m_hProcess == IntPtr.Zero)
                {
                    uint limitedAccess = (uint)(MemoryApi.ProcessAccessType.PROCESS_VM_READ | MemoryApi.ProcessAccessType.PROCESS_QUERY_LIMITED_INFORMATION);
                    this.m_hProcess = MemoryApi.OpenProcess(limitedAccess, false, (uint)this.m_ReadProcess.Id);
                }

                if (this.m_hProcess == IntPtr.Zero)
                {
                    Logger.Warn("ProcessMemoryReader", "OpenProcess failed for PID " + this.m_ReadProcess.Id + " (Win32 error " + Marshal.GetLastWin32Error() + ")");
                    return false;
                }

                return true;
            }
        }

        public List<MemoryRegion> EnumerateWritableRegions(long minAddress, long maxAddress)
        {
            List<MemoryRegion> regions = new List<MemoryRegion>();
            if (minAddress <= 0 || maxAddress <= minAddress)
            {
                return regions;
            }

            IntPtr handle;
            lock (this.handleLock)
            {
                handle = this.m_hProcess;
            }

            if (handle == IntPtr.Zero)
            {
                return regions;
            }

            int mbiSize = Marshal.SizeOf(typeof(MemoryApi.MEMORY_BASIC_INFORMATION));
            long cursor = minAddress;

            while (cursor < maxAddress)
            {
                MemoryApi.MEMORY_BASIC_INFORMATION mbi;
                IntPtr queried = MemoryApi.VirtualQueryEx(handle, (IntPtr)cursor, out mbi, (IntPtr)mbiSize);
                if (queried == IntPtr.Zero)
                {
                    break;
                }

                long regionStart = mbi.BaseAddress.ToInt64();
                long regionSize = mbi.RegionSize.ToInt64();
                if (regionSize <= 0)
                {
                    break;
                }

                long regionEnd = regionStart + regionSize;
                bool isCommitted = (mbi.State & MemoryApi.MEM_COMMIT) != 0;
                bool isGuarded = (mbi.Protect & (MemoryApi.PAGE_GUARD | MemoryApi.PAGE_NOACCESS)) != 0;
                uint baseProtect = mbi.Protect & 0xFFu;
                bool isWritable = baseProtect == MemoryApi.PAGE_READWRITE
                    || baseProtect == MemoryApi.PAGE_WRITECOPY
                    || baseProtect == MemoryApi.PAGE_EXECUTE_READWRITE;

                if (isCommitted && !isGuarded && isWritable)
                {
                    long clampedStart = Math.Max(regionStart, minAddress);
                    long clampedEnd = Math.Min(regionEnd, maxAddress);
                    if (clampedEnd > clampedStart)
                    {
                        long len = Math.Min(clampedEnd - clampedStart, int.MaxValue);
                        regions.Add(new MemoryRegion
                        {
                            BaseAddress = (IntPtr)clampedStart,
                            RegionSize = (int)len
                        });
                    }
                }

                if (regionEnd <= cursor)
                {
                    break;
                }

                cursor = regionEnd;
            }

            return regions;
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
