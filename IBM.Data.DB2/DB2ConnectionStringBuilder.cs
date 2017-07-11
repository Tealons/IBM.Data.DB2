using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBM.Data.DB2
{
    [Obsolete]
    //for legacy purpose
    public class DB2ConnectionStringBuilder : DbConnectionStringBuilder
    {
        public string Database
        {
            get { return "a po co nam to"; }
        }
        public DB2ConnectionStringBuilder(string connectionString)
        {

        }
    }
}
