
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
using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace IBM.Data.DB2
{
	public sealed class DB2Transaction : DbTransaction, IDbTransaction, IDisposable
    {
		private enum TransactionState
		{
			Open,
			Committed,
			Rolledback,
		}
		IsolationLevel isolationLevel;
		DB2Connection connection;
		TransactionState state;
		
		internal DB2Transaction(DB2Connection con, IsolationLevel isoL)
		{
			long db2IsoL;
			connection = con;
			short sqlRet;

			isolationLevel = isoL;

			switch (isoL) 
			{
				default:
				case System.Data.IsolationLevel.Chaos:				//No DB2equivalent, default to SQL_TXN_READ_COMMITTED
				case System.Data.IsolationLevel.ReadCommitted:		//SQL_TXN_READ_COMMITTED
					db2IsoL = DB2Constants.SQL_TXN_READ_COMMITTED;
					break;
				case System.Data.IsolationLevel.ReadUncommitted:	//SQL_TXN_READ_UNCOMMITTED
					db2IsoL = DB2Constants.SQL_TXN_READ_UNCOMMITTED;
					break;
				case System.Data.IsolationLevel.RepeatableRead:		//SQL_TXN_REPEATABLE_READ
					db2IsoL = DB2Constants.SQL_TXN_REPEATABLE_READ;
					break;
				case System.Data.IsolationLevel.Serializable:		//SQL_TXN_SERIALIZABLE_READ
					db2IsoL = DB2Constants.SQL_TXN_SERIALIZABLE_READ;
					break;
			}

            //AutoCommit
            if (connection.AutoCommit)
            {
                sqlRet = DB2CLIWrapper.SQLSetConnectAttr(connection.DBHandle, DB2Constants.SQL_ATTR_AUTOCOMMIT, new IntPtr(DB2Constants.SQL_AUTOCOMMIT_OFF), 0);
                DB2ClientUtils.DB2CheckReturn(sqlRet, DB2Constants.SQL_HANDLE_DBC, connection.DBHandle, "Error setting AUTOCOMMIT OFF in transaction CTOR.", connection);
                connection.AutoCommit = false;
            }
            sqlRet = DB2CLIWrapper.SQLSetConnectAttr(connection.DBHandle, DB2Constants.SQL_ATTR_TXN_ISOLATION, new IntPtr(db2IsoL), 0);
			DB2ClientUtils.DB2CheckReturn(sqlRet, DB2Constants.SQL_HANDLE_DBC, connection.DBHandle, "Error setting isolation level.", connection);

			state = TransactionState.Open;
		}
               
        /// <summary>
        /// IsolationLevel property
        /// </summary>
        /// 
        public override IsolationLevel IsolationLevel
		{
			get 
			{
				CheckStateOpen();
				return isolationLevel;
			}
		}

        public DB2Connection Connection
        {
            get { return connection; }
        }

        protected override DbConnection DbConnection => this.connection;

        internal void CheckStateOpen()
		{
			if(state == TransactionState.Committed)
				throw new InvalidOperationException("Transaction was already committed. It is no longer usable.");
			if(state == TransactionState.Rolledback)
				throw new InvalidOperationException("Transaction was already rolled back. It is no longer usable.");
		}
		
		/// <summary>
		/// Dispose method.
		/// </summary>
		//public override void Dispose()
		//{
		//	if (state != TransactionState.Open) 
		//		return;

		//	Rollback();
		//}

        public override void Commit()
        {
            CheckStateOpen();
            DB2CLIWrapper.SQLEndTran(DB2Constants.SQL_HANDLE_DBC, connection.DBHandle, DB2Constants.SQL_COMMIT);
            this.state = TransactionState.Committed;
            this.connection.TransactionOpen = false;
            this.connection.WeakRefTransaction = null;
            this.connection = null;
        }

        public override void Rollback()
        {
            CheckStateOpen();
            DB2CLIWrapper.SQLEndTran(DB2Constants.SQL_HANDLE_DBC, connection.DBHandle, DB2Constants.SQL_ROLLBACK);
            this.connection.TransactionOpen = false;
            this.state = TransactionState.Rolledback;
            this.connection.WeakRefTransaction = null;
            this.connection = null;
        }
    }
}
