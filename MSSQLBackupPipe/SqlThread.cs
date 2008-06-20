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
using System.Data;
using System.Data.SqlClient;
using System.Threading;

using MSSQLBackupPipe.StdPlugins;

namespace MSSQLBackupPipe
{
    class SqlThread : IDisposable
    {
        private Thread mThread;
        private Exception mException;
        private SqlConnection mCnn;
        private SqlCommand mCmd;
        private bool mDisposed;

        public void PreConnect(string instanceName, string deviceName, IBackupDatabase dbComponent, string dbConfig, bool isBackup)
        {
            try
            {
                string dataSource = string.IsNullOrEmpty(instanceName) ? "." : string.Format(@".\{0}", instanceName);
                mCnn = new SqlConnection(string.Format("Data Source={0};Initial Catalog=master;Integrated Security=SSPI;", dataSource));
                mCnn.Open();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + "\n\nConnection String=" + mCnn.ConnectionString, e);
            }

            mCmd = new SqlCommand();
            mCmd.Connection = mCnn;
            mCmd.CommandTimeout = 0;
            if (isBackup)
            {
                dbComponent.ConfigureBackupCommand(dbConfig, deviceName, mCmd);
            }
            else
            {
                dbComponent.ConfigureRestoreCommand(dbConfig, deviceName, mCmd);
            }

        }
        public void ConnectInAnoterThread()
        {
            ThreadStart job = new ThreadStart(ThreadStart);
            mThread = new Thread(job);
            mThread.Start();
        }
        public Exception WaitForCompletion()
        {
            mThread.Join();
            return mException;
        }


        private void ThreadStart()
        {
            try
            {


                Console.WriteLine("Executing:");
                Console.WriteLine(mCmd.CommandText);

                mCmd.ExecuteNonQuery();

            }
            catch (Exception e)
            {
                mException = e;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~SqlThread()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mDisposed)
            {
                if (disposing)
                {
                    if (mCmd != null)
                    {
                        mCmd.Dispose();
                    }
                    mCmd = null;

                    if (mCnn != null)
                    {
                        mCnn.Dispose();
                    }
                    mCnn = null;

                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            mDisposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Dispose(disposing);

        }

    }
}
