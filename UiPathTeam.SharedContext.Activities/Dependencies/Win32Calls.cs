using System;
using System.Runtime.InteropServices;

namespace UiPathTeam.SharedContext.Dependencies
{
    /*
    class Win32Calls
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateMutex(IntPtr lpMutexAttributes,
            bool bInitialOwner, string lpName);

        [DllImport("kernel32.dll")]
        public static extern bool ReleaseMutex(IntPtr hMutex);

        /// <summary>
        /// This value can be returned by CreateMutex() and is found in
        /// C++ in the error.h header file.
        /// </summary>
        public const int ERROR_ALREADY_EXISTS = 183;

        public static bool TakeMutex(string mutexName)
        {
            IntPtr ipHMutex = new IntPtr(0);

            try
            {
                ipHMutex = Win32Calls._TakeMutex(mutexName);

                // verify its status
                if (ipHMutex == IntPtr.Zero)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        protected static IntPtr _TakeMutex(string mutexName)
        {
            IntPtr ipZero = new IntPtr(0);
            IntPtr ipHMutex = new IntPtr(0);

            // Create the mutex 
            ipHMutex = Win32Calls.CreateMutex(ipZero,
                true, mutexName);

            // verify its status
            if (ipHMutex != IntPtr.Zero)
            {
                // check GetLastError value (MUST use this call. See MSDN)
                int iGLE = Marshal.GetLastWin32Error();

                // if we get the ERROR_ALREADY_EXISTS value, there is
                // already another instance of this application running.
                if (iGLE == Win32Calls.ERROR_ALREADY_EXISTS)
                {
                    // So, don't allow this instance to run.
                    return ipZero;
                }
            }

            return ipHMutex;
        }


        public static void ReleaseMutex(string mutexName)
        {
            IntPtr ipHMutex = Win32Calls._TakeMutex(mutexName);

            if (ipHMutex != (IntPtr)0)
                Win32Calls.ReleaseMutex(ipHMutex);
        }
    }
    */
}
