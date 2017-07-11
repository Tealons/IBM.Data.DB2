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
        //private readonly string connectionString;
        public string Database
        {
            get { return "a po co nam to"; }
        }

        //public DB2ConnectionStringBuilder(string connectionString)
        //{
        //    this.connectionString = connectionString;
        //    Parse();
        //}

        public static Dictionary<string, string> Parse(string connectionString)
        {
            string[] pairs = connectionString.Split(';');
            Dictionary<string, string> configuration = new Dictionary<string, string>(pairs.Length);
            for(int i = 0;i<pairs.Length;i++)
            {
                string[] internalPair = pairs[i].Split('=');
                configuration.Add(internalPair[0], internalPair[1]);
            }

            return configuration;
        }
    }
}
