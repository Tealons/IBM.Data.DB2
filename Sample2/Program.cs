using IBM.Data.DB2;
using IniParser;
using IniParser.Model;
using System;
using System  .Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sample2
{
    class Program
    {
        static string currentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
        static FileIniDataParser fileIniData = new FileIniDataParser();
        
        static IniData _parsedData;
        static IniData IniData
        {
            get
            {  if(_parsedData == null)
                    _parsedData = fileIniData.ReadFile(Path.Combine(currentDirectory,"databases.ini"));

                return _parsedData;
            }
        }

        static string _cliDSN;
        static string _cli;

        static void Main(string[] args)
        {
            fileIniData.Parser.Configuration.CommentString = "#";

            _cliDSN = IniData["Databases"]["CLIDSN"];
            _cli = IniData["Databases"]["ADONET"];

            TransactionTest();           
        }        

        public static void TransactionTest()
        {
            //IniData parsedData = fileIniData.ReadFile();
            try
            {
                DB2Connection connection = new DB2Connection(_cli);
                try
                {
                    connection.Open();
                }
                catch (DB2Exception e)
                {

                }

                //Thread.Sleep(12000);

                DB2Command cmd = new DB2Command();

                cmd.Connection = connection;
                cmd.CommandText = "select * from DB2INST1.ACT where actno = 10";

                DB2Command cmd1 = new DB2Command();
                cmd1.Connection = connection;
                cmd1.CommandText = "insert into DB2INST1.ACT values(182,DCO1','DDDD')";

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        cmd.Transaction = transaction;
                        //cmd1.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        //cmd1.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }

        static long Orginal_IBM_Provider()
        {
            Stopwatch watch = Stopwatch.StartNew();            
            DB2Connection connection = new DB2Connection(_cliDSN);
            connection.Open();
            DB2Command cmd = new DB2Command();
            cmd.Connection = connection;
            //cmd.CommandText = "select * from syscat.columns";
            var parameter = cmd.CreateParameter();
            parameter.ParameterName = "@TABSCHEMA";
            parameter.Value = "SYSTOOLS";
            //cmd.Parameters.Add(parameter);
            //DB2DataAdapter da = new DB2DataAdapter(cmd);
            cmd.CommandText = "select XML1 from DB2INST1.STRINGTYPES1";
            
            //cmd.CommandText = "SELECT a.PKID, a.TblType, a.Code, a.Description, a.AmountValue, a.LegacyCode, a.IsSysTable, a.GroupCode, a.UpdUserID, a.UpdDatetime, a.UpdNumber, a.Code || ' - ' || a.Description as CodeAndDescription, (CASE WHEN b.Description IS NULL THEN '' ELSE b.Description END) as TblTypeDesc  FROM AP.Lookup a LEFT OUTER JOIN AP.Lookup b ON(b.TblType = 'TBL_TYPE' and b.Code = a.TblType) WHERE a.IsActive = 'Y' ORDER BY a.Description";
            //DataTable dt = new DataTable();
            //da.Fill(dt);
            
            var reader = cmd.ExecuteReader();
            var schema = reader.GetSchemaTable();
            while (reader.Read())
            {
                var r = reader.GetName(0);
                var xml = reader.GetString(0);
            }

            //DB2DataAdapter da = new DB2DataAdapter(cmd);
            //DataSet ds = new DataSet();
            //da.Fill(ds);
            //watch.Stop();

            return watch.ElapsedMilliseconds;
        }
    }
}
