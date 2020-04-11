using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data.SQLite;
namespace TimeSync
{
    
    class TimeStampUtil
    {
        
        private static TimeStampUtil mInstance = null;
        public static TimeStampUtil getInstance()
        {
            if (mInstance == null) mInstance = new TimeStampUtil();
            return mInstance;
        }
        public TimeStampUtil()
        {
            init();
            ReloadHostsAndPorts();
        }
        private void init()
        {
            mHostList = new ArrayList();
            mPortList = new ArrayList();
            hostport_semaphore = new System.Threading.Semaphore(1, 1);
            //String strconn = "Data Source="+AppDomain.CurrentDomain.BaseDirectory + "ntp_servers.db"; //@"Data Source=C:\Users\황세현삼성놋9\Desktop\TimeSync\TimeSync\bin\Debug\ntp_servers.db";
            sqlite_conn = new NTPServersManager().getConnection(); //new SQLiteConnection(strconn);
        }
        public void onDestroy()
        {
            hostport_semaphore.WaitOne();
            try
            {
                sqlite_conn.Close();
            }
            catch(Exception e)
            {

            }
            mHostList.Clear();
            mHostList = null;
            mPortList.Clear();
            mPortList = null;
            hostport_semaphore.Release();
            hostport_semaphore.Dispose();
            hostport_semaphore.Close();
            hostport_semaphore = null;
        }
        SQLiteConnection sqlite_conn;
        ArrayList mHostList, mPortList;
        public void RequestReloadingHostsAndPorts()
        {
            ReloadHostsAndPorts();
        }
        private void ReloadHostsAndPorts()
        {
            if (mHostList == null)
                mHostList = new ArrayList();
            if (mPortList == null)
                mPortList = new ArrayList();
            if (hostport_semaphore == null) return;
            hostport_semaphore.WaitOne();
            mHostList.Clear();
            mPortList.Clear();
            sqlite_conn.Open();
            SQLiteCommand cmd;
            SQLiteDataReader reader;
            cmd = new SQLiteCommand("select host from hosts order by is_default desc,id", sqlite_conn);

            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                mHostList.Add(reader["host"]);
                //Console.WriteLine(reader["host"]);
            }
            reader.Close();
            cmd = new SQLiteCommand("select port from ports order by port desc", sqlite_conn);
            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                //Console.WriteLine(reader["port"].GetType().Name);
                mPortList.Add(Convert.ToInt32(reader["port"]));
                //Console.WriteLine(reader["port"]);
            }
            reader.Close();
            sqlite_conn.Close();
            hostport_semaphore.Release();
        }
        private System.Threading.Semaphore hostport_semaphore;
        public ulong Convert2TimeStampFromSystemTimeStruct(in SYSTEMTIME stru)
        {
            ulong rst = 0;
            DateTime dt = new DateTime(stru.wYear,stru.wMonth,stru.wDay,stru.wHour,stru.wMinute,stru.wSecond);
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            rst = Convert.ToUInt64( (dt - origin).TotalSeconds);
            return rst;
        }
        public DateTime ConvertFromUnixTimestamp(ulong timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }
        public T ByteToStruct<T>(byte[] buffer) where T : struct

        {

            int size = Marshal.SizeOf(typeof(T));



            if (size > buffer.Length)

            {

                throw new Exception();

            }



            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(buffer, 0, ptr, size);

            T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));

            Marshal.FreeHGlobal(ptr);

            return obj;

        }
        public ulong getTimeStampFromNetworks()
        {

            ulong rst = 0;
            hostport_semaphore.WaitOne();
            for (int tried = 1; tried <= 3; tried++)
            {
                for (int i = 0; i < mHostList.Count; i++)
                {
                    String host = (String)mHostList[i];
                    for (int j = 0; j < mPortList.Count; j++)
                    {
                        try
                        {

                            int port = (int)mPortList[j];
                            rst = getTimeStampFromNetwork(host, port);
                            if (rst > 0) {
                                hostport_semaphore.Release();
                                return rst; }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                    }
                }
            }
            hostport_semaphore.Release();
            return rst;
        }
        public ulong getTimeStampFromNetwork(String host, int port)
        {
            IPAddress[] iPAddresses = Dns.GetHostAddresses(host);
            if (iPAddresses.Length < 1) return 0;
            IPAddress mIp = iPAddresses[0];
            UdpClient client = new UdpClient();
            client.Client.SendTimeout = 10 * 1000;
            client.Client.ReceiveTimeout = 15 * 1000;
            byte[] request_packet = new byte[48];
            request_packet[0] = 0b11100011;
            request_packet[1] = 0;
            request_packet[2] = 6;
            request_packet[3] = 0xEC;
            request_packet[12] = 49;
            request_packet[13] = 0x4E;
            request_packet[14] = 49;
            request_packet[15] = 52;
            try
            {
                client.Send(request_packet, request_packet.Length, new IPEndPoint(mIp, port));
                IPEndPoint epRemote = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = client.Receive(ref epRemote);
                Console.WriteLine(bytes.ToString());
                Console.WriteLine(bytes.Length.ToString());
                TimeStruct ts = ByteToStruct<TimeStruct>(bytes);
                ulong tmp = ts.rcv_tm;
                ulong tmp2 =  /*((ulong)IPAddress.NetworkToHostOrder((int)(tmp >> 32)));+*/ (((ulong)IPAddress.NetworkToHostOrder((int)(tmp/*&0xFFFFFFFF*/))) << 0) & 0xFFFFFFFF;
                tmp2 = tmp2 - TIME_PREFIX;

                return tmp2;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }
        private static ulong TIME_PREFIX/*SECONDS_SINCE_FIRST_EPOCH */ = 2208988800l;
        public SYSTEMTIME ConvertTimeStamp2SYSTEMTIME_struct(ulong ts)
        {
            DateTime dt = ConvertFromUnixTimestamp(ts);
            SYSTEMTIME st = new SYSTEMTIME();
            st.wHour = (ushort)dt.Hour;
            st.wMinute = (ushort)dt.Minute;
            st.wSecond = (ushort)dt.Second;
            st.wYear = (ushort)dt.Year;
            st.wMonth = (ushort)dt.Month;
            st.wDay = (ushort)dt.Day;
            return st;
        }
    }
}
