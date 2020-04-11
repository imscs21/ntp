using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Collections;
using System.Threading;
using System.Data;

namespace TimeSync
{
    class NTPServersManager
    {
        SQLiteConnection sqlite_conn;
        Semaphore semaphore;
        public NTPServersManager()
        {
            semaphore = new Semaphore(1, 1);
            String strconn = "Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "ntp_servers.db"; //@"Data Source=C:\Users\황세현삼성놋9\Desktop\TimeSync\TimeSync\bin\Debug\ntp_servers.db";
            sqlite_conn = new SQLiteConnection(strconn);

        }
        public SQLiteConnection getConnection()
        {
            return sqlite_conn;
        }
        public SQLiteConnection getSingleConnection()
        {
            SQLiteConnection rst =  getConnection();
            semaphore.WaitOne();
            sqlite_conn.Open();
            return rst;
        }
        public void CloseSingle()
        {
            sqlite_conn.Close();
            semaphore.Release();
        }
        public void UpdateInsertDeleteData(string sql)
        {
            semaphore.WaitOne();
            sqlite_conn.Open();
            using (var tran = sqlite_conn.BeginTransaction())
            {
                try
                {
                    SQLiteCommand command = new SQLiteCommand(sql, sqlite_conn);
                    command.ExecuteNonQuery();
                    tran.Commit();
                }catch(Exception e)
                {
                    tran.Rollback();
                }
            }
            sqlite_conn.Close();
            semaphore.Release();
        }
        
        public DataTable getHostsGridData()
        {
            DataTable rst = new DataTable();
            SQLiteDataAdapter adapter;
            semaphore.WaitOne();
            sqlite_conn.Open();
            adapter = new SQLiteDataAdapter("select * from hosts", getConnection());
            adapter.Fill(rst);
            sqlite_conn.Close();
            semaphore.Release();
            return rst;
        }
        public DataTable getPortsGridData()
        {
            DataTable rst = new DataTable();
            SQLiteDataAdapter adapter;
            semaphore.WaitOne();
            sqlite_conn.Open();
            adapter = new SQLiteDataAdapter("select * from ports", getConnection());
            adapter.Fill(rst);
            sqlite_conn.Close();
            semaphore.Release();
            return rst;
        }
    }
}
