
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace IBM.Data.DB2
{

    internal sealed class DB2OpenConnection : IDisposable
    {
        private IntPtr dbHandle = IntPtr.Zero;
        
        private bool disposed = false;        
        public bool transactionOpen;
        public bool autoCommit = true;
        private string databaseProductName;
        private string databaseVersion;

        public IntPtr DBHandle
        {
            get { return dbHandle; }
        }
        public string DatabaseProductName
        {
            get { return databaseProductName; }
        }
        public string DatabaseVersion
        {
            get { return databaseVersion; }
        }

        public string SQLGetInfo(IntPtr dbHandle, short infoType)
        {
            StringBuilder sb = new StringBuilder(DB2Constants.SQL_MAX_OPTION_STRING_LENGTH);
            short stringLength;
            DB2Constants.RetCode sqlRet = (DB2Constants.RetCode)DB2CLIWrapper.SQLGetInfo(dbHandle, infoType, sb, DB2Constants.SQL_MAX_OPTION_STRING_LENGTH, out stringLength);

            if (sqlRet != DB2Constants.RetCode.SQL_SUCCESS && sqlRet != DB2Constants.RetCode.SQL_SUCCESS_WITH_INFO)
                throw new DB2Exception(DB2Constants.SQL_HANDLE_DBC, dbHandle, "SQLGetInfo Error");

            return sb.ToString().Trim();
        }

        public DB2OpenConnection(string connnectionString, DB2Connection connection)
        {            
            try
            {
                DB2Constants.RetCode sqlRet = (DB2Constants.RetCode)DB2CLIWrapper.SQLAllocHandle(DB2Constants.SQL_HANDLE_DBC, DB2Environment.Instance.penvHandle, out dbHandle);
                DB2ClientUtils.DB2CheckReturn(sqlRet, DB2Constants.SQL_HANDLE_DBC, DB2Environment.Instance.penvHandle, "Unable to allocate database handle in DB2Connection.", connection);

                StringBuilder outConnectStr = new StringBuilder(DB2Constants.SQL_MAX_OPTION_STRING_LENGTH);
                short numOutCharsReturned;

                sqlRet = (DB2Constants.RetCode)DB2CLIWrapper.SQLDriverConnect(dbHandle, IntPtr.Zero,
                    connnectionString, DB2Constants.SQL_NTS,
                    outConnectStr, DB2Constants.SQL_MAX_OPTION_STRING_LENGTH, out numOutCharsReturned,
                    DB2Constants.SQL_DRIVER_NOPROMPT);

                DB2ClientUtils.DB2CheckReturn(sqlRet, DB2Constants.SQL_HANDLE_DBC, dbHandle, "Unable to connect to the database.", connection);

                databaseProductName = SQLGetInfo(dbHandle, DB2Constants.SQL_DBMS_NAME);
                databaseVersion = SQLGetInfo(dbHandle, DB2Constants.SQL_DBMS_VER);
            }
            catch
            {
                if (dbHandle != IntPtr.Zero)
                {
                    DB2CLIWrapper.SQLFreeHandle(DB2Constants.SQL_HANDLE_DBC, dbHandle);
                    dbHandle = IntPtr.Zero;
                }
                throw;
            }
        }

        public void RollbackDeadTransaction()
        {
            DB2CLIWrapper.SQLEndTran(DB2Constants.SQL_HANDLE_DBC, DBHandle, DB2Constants.SQL_ROLLBACK);
            transactionOpen = false;
        }

        public void Close()
        {
            if (transactionOpen)
                RollbackDeadTransaction();


            Dispose();

        }

        private void FreeHandles()
        {
            if (dbHandle != IntPtr.Zero)
            {
                short sqlRet = DB2CLIWrapper.SQLDisconnect(dbHandle);
                // Note that SQLDisconnect() automatically drops any statements and
                // descriptors open on the connection.
                sqlRet = DB2CLIWrapper.SQLFreeHandle(DB2Constants.SQL_HANDLE_DBC, dbHandle);

                dbHandle = IntPtr.Zero;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // dispose managed resources
                }
                FreeHandles();
            }
            disposed = true;
        }

        ~DB2OpenConnection()
        {          
            Dispose(false);
        }
        #endregion
    }
}