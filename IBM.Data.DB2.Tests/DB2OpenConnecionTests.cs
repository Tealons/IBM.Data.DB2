using System;
using System.Linq;
using Xunit;

using Moq;
using System.Collections.Generic;

namespace IBM.Data.DB2.Tests
{
    public class DB2OpenConnecionTests
    {        
        public static IEnumerable<object[]> GetSQL_INVALID_HANDLE()
        {
            yield return new object[] { DB2Constants.RetCode.SQL_INVALID_HANDLE, 1, IntPtr.Zero, "", null };            
        }

        public static IEnumerable<object[]> GetSQL_SUCCESS()
        {
            yield return new object[] { DB2Constants.RetCode.SQL_SUCCESS, 1, IntPtr.Zero, "", null };
        }


        public static IEnumerable<object[]> GetSQL_ERROR()
        {
            yield return new object[] { DB2Constants.RetCode.SQL_ERROR, 1, IntPtr.Zero, "", null };
        }

        [Theory]
        [MemberData(nameof(GetSQL_INVALID_HANDLE))]        
        public void DB2ClientUtils_DB2CheckReturn_should_throw_argument_exception(short sqlRet, short handleType, IntPtr handle, string message, DB2Connection connection)
        {            
            Assert.Throws<ArgumentException>(() => DB2ClientUtils.DB2CheckReturn(sqlRet, handleType, handle, message, connection));
        }

        [Theory]
        [InlineData(DB2Constants.RetCode.SQL_ERROR)]
        public void DB2ClientUtils_DB2CheckReturn_should_throw_db2_exception(short sqlRet)
        {          
            Assert.Throws<DB2Exception>(() =>
            {                
                //DB2Connection connection = new DB2Connection(wrongconnectionstring);
                //connection.Open();                
            });
        }
    }
}
