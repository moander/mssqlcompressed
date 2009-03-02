/*
	Copyright 2008 Clay Lenhart <clay@lenharts.net>


	This file is part of MSSQL Compressed Backup.

    MSSQL Compressed Backup is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Foobar is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;

using VirtualBackupDevice;
using MSSQLBackupPipe.StdPlugins;

namespace MSSQLBackupPipe
{
    class DeviceThread : IDisposable
    {

        private bool mDisposed;
        private Stream mTopOfPipeline = null;
        private VirtualDevice mDevice = null;
        private VirtualDeviceSet mDeviceSet = null;
        private bool mIsBackup;


        private Thread mThread;
        private Exception mException;

        public void Initialize(bool isBackup, Stream topOfPipeline, VirtualDevice device, VirtualDeviceSet deviceSet)
        {

            mIsBackup = isBackup;
            mTopOfPipeline = topOfPipeline;
            mDevice = device;
            mDeviceSet = deviceSet;
        }

        public void BeginCopy()
        {

            ThreadStart job = new ThreadStart(ThreadStart);
            mThread = new Thread(job);
            mThread.Start();


        }

        public Exception EndCopy()
        {
            mThread.Join();
            return mException;
        }

        private void ThreadStart()
        {
            try
            {


                CommandBuffer buff = new CommandBuffer();

                try
                {
                    ReadWriteData(mDevice, buff, mTopOfPipeline, mIsBackup);
                }
                catch (Exception)
                {
                    mDeviceSet.SignalAbort();
                    throw;
                }


            }
            catch (Exception e)
            {
                mException = e;
            }
        }



        private static void ReadWriteData(VirtualDevice device, CommandBuffer buff, Stream stream, bool isBackup)
        {

            bool timedOut;
            while (device.GetCommand(null, buff, out timedOut))
            {
                if (!timedOut)
                {
                    CompletionCode completionCode = CompletionCode.DISK_FULL;
                    uint bytesTransferred = 0;

                    try
                    {

                        switch (buff.CommandType)
                        {
                            case DeviceCommandType.Write:

                                if (!isBackup)
                                {
                                    throw new InvalidOperationException("Cannot write in 'restore' mode");
                                }

                                bytesTransferred = (uint)buff.WriteToStream(stream);

                                completionCode = CompletionCode.SUCCESS;

                                break;
                            case DeviceCommandType.Read:

                                if (isBackup)
                                {
                                    throw new InvalidOperationException("Cannot read in 'backup' mode");
                                }

                                bytesTransferred = (uint)buff.ReadFromStream(stream);

                                if (bytesTransferred > 0)
                                {
                                    completionCode = CompletionCode.SUCCESS;
                                }
                                else
                                {
                                    completionCode = CompletionCode.HANDLE_EOF;
                                }

                                break;
                            case DeviceCommandType.ClearError:
                                completionCode = CompletionCode.SUCCESS;
                                break;

                            case DeviceCommandType.Flush:
                                stream.Flush();
                                completionCode = CompletionCode.SUCCESS;
                                break;

                            default:
                                throw new ArgumentException(string.Format("Unknown command: {0}", buff.CommandType));
                        }
                    }
                    finally
                    {
                        device.CompleteCommand(buff, completionCode, bytesTransferred, (ulong)0);
                    }
                }

            }
        }






        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        ~DeviceThread()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mDisposed)
            {
                if (disposing)
                {
                    // dispose of managed resources
                    if (mDevice != null)
                    {
                        mDevice = null;
                    }

                    mDeviceSet = null;

                    // the Program class will dispose of the streams

                    //if (mTopOfPipeline != null)
                    //{
                    //    mTopOfPipeline.Dispose();
                    //    mTopOfPipeline = null;
                    //}
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            mDisposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Dispose(disposing);

        }


        #endregion
    }
}
