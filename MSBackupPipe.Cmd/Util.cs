using System;
using System.Collections.Generic;
using System.Text;

namespace MSBackupPipe.Cmd
{
    internal static class Util
    {
        public static void WriteError(Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine();
            Console.WriteLine(e.GetType().FullName);
            Console.WriteLine(e.StackTrace);
            if (e.InnerException != null)
            {
                WriteError(e.InnerException);
            }
        }
    }
}
