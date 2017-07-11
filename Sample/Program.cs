using IBM.Data.DB2;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static string currentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
        static FileIniDataParser fileIniData = new FileIniDataParser();

        static IniData _parsedData;
        static IniData IniData
        {
            get
            {
                if (_parsedData == null)
                    _parsedData = fileIniData.ReadFile(Path.Combine(currentDirectory, "databases.ini"));

                return _parsedData;
            }
        }

        static string _adonet;        

        static void Main(string[] args)
        {
            fileIniData.Parser.Configuration.CommentString = "#";

            _adonet = IniData["Databases"]["ADONET"];            

            TransactionTest();
        }

        public static void TransactionTest()
        {            
            DB2Connection connection = new DB2Connection(_adonet);
            connection.Open();
            DB2Command cmd = new DB2Command();

            cmd.Connection = connection;
            cmd.CommandText = "delete from DB2INST1.ACT where actno = 10";

            DB2Command cmd1 = new DB2Command();
            cmd1.Connection = connection;
            cmd1.CommandText = "insert into DB2INST1.ACT values(182,'DCO1','DDDD')";

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    //cmd.Transaction = transaction;
                    //cmd1.Transaction = transaction;
                    cmd.ExecuteNonQuery();
                    cmd1.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch(Exception e)
                {
                    transaction.Rollback();
                }
            }
        }
    }
}
