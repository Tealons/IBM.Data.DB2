
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
using System.Data;
using System.Data.Common;
using System.Text;

namespace IBM.Data.DB2
{
    public class DB2Connection : DbConnection, IDbConnection, IDisposable
    {
        private IntPtr dbHandle = IntPtr.Zero;
        private bool transactionOpen;
        private ArrayList refCommands;
        private int connectionTimeout;
        private WeakReference refTransaction;
        private bool disposed = false;
        private string connectionString;
        private string databaseProductName;
        private string databaseVersion;
        private bool nativeOpenPerformed;
        private bool autoCommit = true;
        private DB2OpenConnection openConnection;
        private DB2ConnectionSettings connectionSettings;

        public override string Database
        {
            get { return databaseProductName; }
        }

        public IntPtr DBHandle
        {
            get { return openConnection.DBHandle; }
        }

        public bool TransactionOpen
        {
            get { return transactionOpen; }
            set { transactionOpen = value; }
        }


        public bool AutoCommit
        {
            get { return autoCommit; }
            set { autoCommit = value; }
        }

        public DB2Connection()
        {

        }

        #region Constructors

        public DB2Connection(string connectionString)
        {
            //this.connectionString = connectionString;
            SetConnectionString(connectionString);
        }

        #endregion

        #region ConnectionString property

        public override string ConnectionString
        {
            get
            {
                return connectionString;
            }
            set
            {
                connectionString = value;
            }
        }
        #endregion

        #region State property

        public override int ConnectionTimeout
        {
            get
            {
                return connectionTimeout;
            }            
        }

        public override ConnectionState State
        {
            get
            {               
                if (!nativeOpenPerformed)
                    return ConnectionState.Closed;
                
                int isDead;
                DB2Constants.RetCode sqlRet = (DB2Constants.RetCode)DB2CLIWrapper.SQLGetConnectAttr(openConnection.DBHandle, DB2Constants.SQL_ATTR_CONNECTION_DEAD, out isDead, 0, IntPtr.Zero);

                DB2ClientUtils.DB2CheckReturn(sqlRet, DB2Constants.SQL_HANDLE_DBC, openConnection.DBHandle, "Unable to connect to the database.", this);

                if (((sqlRet == DB2Constants.RetCode.SQL_SUCCESS_WITH_INFO) || (sqlRet == DB2Constants.RetCode.SQL_SUCCESS)) && (isDead == DB2Constants.SQL_CD_FALSE))
                {
                    return ConnectionState.Open;
                }

                return ConnectionState.Closed;
            }
        }
        #endregion

        #region events

        public event DB2InfoMessageEventHandler InfoMessage;
        public event StateChangeEventHandler StateChange;

        internal void OnInfoMessage(short handleType, IntPtr handle)
        {
            if (InfoMessage != null)
            {
                // Don't get error information until we know for sure someone is listening
                try
                {
                    InfoMessage(this,
                        new DB2InfoMessageEventArgs(new DB2ErrorCollection(handleType, handle)));
                }
                catch (Exception)
                { }
            }
        }

        private void OnStateChange(StateChangeEventArgs args)
        {
            if (StateChange != null)
                StateChange(this, args);
        }

        #endregion

        #region BeginTransaction Method

        public DB2Transaction BeginTransaction()
        {
            return InternalBeginTransaction(IsolationLevel.ReadCommitted);
        }

        public DB2Transaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return InternalBeginTransaction(isolationLevel);
        }

        public DB2Transaction InternalBeginTransaction()
        {
            return InternalBeginTransaction(IsolationLevel.ReadCommitted);
        }

        public DB2Transaction InternalBeginTransaction(IsolationLevel isolationLevel)
        {
            if ((refTransaction != null) && (refTransaction.IsAlive))
                throw new InvalidOperationException("Cannot open another transaction");
            if (State != ConnectionState.Open)
                throw new InvalidOperationException("BeginTransaction needs an open connection");

            if (refTransaction != null)
            {
                if (refTransaction.IsAlive)
                    throw new InvalidOperationException("Parallel transactions not supported");

                RollbackDeadTransaction();
                refTransaction = null;
            }
            transactionOpen = true;
            DB2Transaction tran = new DB2Transaction(this, isolationLevel);
            refTransaction = new WeakReference(tran);
            return tran;
        }

        #endregion

        //TODO ChangeDatabase
        #region ChangeDatabase
        unsafe public override void ChangeDatabase(string newDBName)
        {
            if (connectionSettings == null)
            {
                throw new InvalidOperationException("No connection string");
            }
            Close();

            SetConnectionString(connectionSettings.ConnectionString.Replace(connectionSettings.DatabaseAlias, newDBName));

            Open();
        }
        #endregion
        
        //TODO Create pool functionality
        public static void ReleaseObjectPool()
        {
            DB2Environment.Instance.Dispose();
        }        
        
        public override void Close()
        {
            DB2Transaction transaction = null;
            if (refTransaction != null)
                transaction = (DB2Transaction)refTransaction.Target;
            if ((transaction != null) && refTransaction.IsAlive)
            {
                transaction.Dispose();
            }
            if (refCommands != null)
            {
                for (int i = 0; i < refCommands.Count; i++)
                {
                    DB2Command command = null;
                    if (refCommands[i] != null)
                    {
                        command = (DB2Command)((WeakReference)refCommands[i]).Target;
                    }
                    if ((command != null) && ((WeakReference)refCommands[i]).IsAlive)
                    {
                        try
                        {
                            command.ConnectionClosed();
                        }
                        catch { }
                    }
                    //?? refCommands[i] = null;
                }
            }

            InternalClose();
        }

        public void InternalClose()
        {
            if (transactionOpen)
                RollbackDeadTransaction();

            FreeHandles();
        }

        private void SetConnectionString(string connectionString)
        {            
            this.connectionSettings = DB2ConnectionSettings.GetConnectionSettings(connectionString);
        }

        public DB2Command CreateCommand()
        {
            return new DB2Command(null, this);
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

        #region Open

        //private void InternalOpen()
        //{
        //    try
        //    {
        //        DB2Constants.RetCode sqlRet = (DB2Constants.RetCode)DB2CLIWrapper.SQLAllocHandle(DB2Constants.SQL_HANDLE_DBC, DB2Environment.Instance.PenvHandle, out dbHandle);
        //        DB2ClientUtils.DB2CheckReturn(sqlRet, DB2Constants.SQL_HANDLE_DBC, DB2Environment.Instance.PenvHandle, "Unable to allocate database handle in DB2Connection.", this);

        //        StringBuilder outConnectStr = new StringBuilder(DB2Constants.SQL_MAX_OPTION_STRING_LENGTH);
        //        short numOutCharsReturned;

        //        sqlRet = (DB2Constants.RetCode)DB2CLIWrapper.SQLDriverConnect(dbHandle, IntPtr.Zero,
        //            connectionString, DB2Constants.SQL_NTS,
        //            outConnectStr, DB2Constants.SQL_MAX_OPTION_STRING_LENGTH, out numOutCharsReturned,
        //            DB2Constants.SQL_DRIVER_NOPROMPT);

        //        DB2ClientUtils.DB2CheckReturn(sqlRet, DB2Constants.SQL_HANDLE_DBC, dbHandle, "Unable to connect to the database.", this);

        //        databaseProductName = SQLGetInfo(dbHandle, DB2Constants.SQL_DBMS_NAME);
        //        databaseVersion = SQLGetInfo(dbHandle, DB2Constants.SQL_DBMS_VER);

        //        /* Set the attribute SQL_ATTR_XML_DECLARATION to skip the XML declaration from XML Data */
        //        sqlRet = (DB2Constants.RetCode)DB2CLIWrapper.SQLSetConnectAttr(dbHandle, DB2Constants.SQL_ATTR_XML_DECLARATION, new IntPtr(DB2Constants.SQL_XML_DECLARATION_NONE), DB2Constants.SQL_NTS);
        //        DB2ClientUtils.DB2CheckReturn(sqlRet, DB2Constants.SQL_HANDLE_DBC, dbHandle, "Unable to set SQL_ATTR_XML_DECLARATION", this);

        //        nativeOpenPerformed = true;
        //    }
        //    catch
        //    {
        //        if (dbHandle != IntPtr.Zero)
        //        {
        //            DB2CLIWrapper.SQLFreeHandle(DB2Constants.SQL_HANDLE_DBC, dbHandle);
        //            dbHandle = IntPtr.Zero;
        //        }
        //        throw;
        //    }
        //}

        public override void Open()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("DB2Connection");
            }

            if (this.State == ConnectionState.Open)
            {
                throw new InvalidOperationException("Connection already open");
            }

            try
            {
                //InternalOpen();
                openConnection = connectionSettings.GetRealOpenConnection(this);
            }
            catch (DB2Exception)
            {
                Close();
                throw;
            }
        }
        #endregion

        public void RollbackDeadTransaction()
        {
            DB2CLIWrapper.SQLEndTran(DB2Constants.SQL_HANDLE_DBC, dbHandle, DB2Constants.SQL_ROLLBACK);
            transactionOpen = false;
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

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Close();                
            }
        }

        ~DB2Connection()
        {
            Dispose(false);
        }
        #endregion

        private void CheckState()
        {
            if (ConnectionState.Closed == State)
                throw new InvalidOperationException("Connection is currently closed.");
        }

        internal WeakReference WeakRefTransaction
        {
            get
            {
                return refTransaction;
            }
            set
            {
                refTransaction = value;
            }

        }
        
        public override string DataSource
        {
            get { return SQLGetInfo(dbHandle, DB2Constants.SQL_DBMS_NAME); }
            
        }

        public override string ServerVersion
        {
            get {
                return SQLGetInfo(dbHandle, DB2Constants.SQL_DBMS_VER);
            }            
        }

        internal void AddCommand(DB2Command command)
        {
            if (refCommands == null)
            {
                refCommands = new ArrayList();
            }
            for (int i = 0; i < refCommands.Count; i++)
            {
                WeakReference reference = (WeakReference)refCommands[i];
                if ((reference == null) || !reference.IsAlive)
                {
                    refCommands[i] = new WeakReference(command);
                    return;
                }
            }
            refCommands.Add(new WeakReference(command));
        }

        internal void RemoveCommand(DB2Command command)
        {
            for (int i = 0; i < refCommands.Count; i++)
            {
                WeakReference reference = (WeakReference)refCommands[i];
                if (object.ReferenceEquals(reference, command))
                {
                    refCommands[i] = null;
                    return;
                }
            }
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return InternalBeginTransaction(isolationLevel);
        }       

        protected override DbCommand CreateDbCommand()
        {            
            DbCommand dbCommand = new DB2Command(string.Empty, this);
            return dbCommand;
        }       

        public bool IsOpen
        {
           get { return State == ConnectionState.Open; }
        }

        public bool NativeOpenPerformed { get { return nativeOpenPerformed; } set {  nativeOpenPerformed = value; } }
    }
}

