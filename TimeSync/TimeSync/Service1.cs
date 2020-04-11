using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Collections;


//using System.Windows.Forms;
//using System.Threading;
public enum ServiceState
{
    SERVICE_STOPPED = 0x00000001,
    SERVICE_START_PENDING = 0x00000002,
    SERVICE_STOP_PENDING = 0x00000003,
    SERVICE_RUNNING = 0x00000004,
    SERVICE_CONTINUE_PENDING = 0x00000005,
    SERVICE_PAUSE_PENDING = 0x00000006,
    SERVICE_PAUSED = 0x00000007
}
/*public enum ServiceCommand
{
    SERIVCECMD_RELOADHOSTLIST = 123,
    SERVICECMD_RELOAD_UPDATE_PRIOD = 124
}*/
[StructLayout(LayoutKind.Sequential)]
public struct ServiceStatus
{
    public int dwServiceType;
    public ServiceState dwCurrentState;
    public int dwControlsAccepted;
    public int dwWin32ExitCode;
    public int dwServiceSpecificExitCode;
    public int dwCheckPoint;
    public int dwWaitHint;
};
[StructLayout(LayoutKind.Sequential)]
public struct TimeStruct
{

    public uint first_header;
    public uint root_delay;
    public uint root_dispersion;
    public uint ref_id;
    public ulong ref_tm;
    public ulong origin_tm;
    public ulong rcv_tm;
    public ulong trans_tm;
    
};
namespace TimeSync
{
    public enum ServiceCommand
    {
        SERIVCECMD_RELOADHOSTLIST = 130,
        SERVICECMD_RELOAD_UPDATE_PRIOD = 131
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEMTIME
    {
        public UInt16 wYear;
        public UInt16 wMonth;
        public UInt16 wDayOfWeek;
        public UInt16 wDay;
        public UInt16 wHour;
        public UInt16 wMinute;
        public UInt16 wSecond;
        public UInt16 wMilliseconds;
    }
    
    public partial class Service1 : ServiceBase
    {
        [DllImport("kernel32")]
        protected static extern bool SetSystemTime(in SYSTEMTIME systemTime);
        [DllImport("kernel32")]
        protected static extern void GetSystemTime(out SYSTEMTIME systemTime);
        private System.Diagnostics.EventLog eventLog1;
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
        
        private TimeStampUtil tsu = null;
        private TimeStampRegistryUtil tsru = null;
        //private NTPServersManager ntpsm;
        private ServiceLogController slc;
        private void init()
        {
            //String strconn = @"Data Source=C:\Users\HSH\source\repos\TimeSync\TimeSync\bin\Debug\ntp_servers.db";
            tsu = TimeStampUtil.getInstance();
            tsru = TimeStampRegistryUtil.getInstance();
            //ntpsm = new NTPServersManager();
            slc = ServiceLogController.getInstance();
            
            system_time = new SYSTEMTIME();
            
        }
        
        
        public Service1()
        {
            InitializeComponent();
            this.init();
            eventLog1 = new System.Diagnostics.EventLog();
            /*
                if (!System.Diagnostics.EventLog.SourceExists("MySource"))
                {
                    System.Diagnostics.EventLog.CreateEventSource(
                        "MySource", "MyNewLog");
                }
            */
            eventLog1.Source = "MySource";
            eventLog1.Log = "MyNewLog";
        }
        Timer tm=null;
        public void ForceRun(string[] args)
        {
            OnStart(args);
            //System.Threading.Thread.Sleep(10 * 1000);
            tm = new Timer(10*60 * 1000);
            tm.Elapsed += (s, e) => { OnStop(); };
            tm.AutoReset = false;
            tm.Start();
            
        }
        SYSTEMTIME system_time;
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void ApplyOfflineTimeStamp()
        {
            ulong tmp = tsru.GetSavedTimeStamp();
            GetSystemTime(out system_time);
            ulong current_system_time = tsu.Convert2TimeStampFromSystemTimeStruct(system_time);
            if (tmp > current_system_time)
            {
                SYSTEMTIME st1 = tsu.ConvertTimeStamp2SYSTEMTIME_struct(tmp);
                SetSystemTime(st1);
            }
        }
        protected void ApplyTimeStamp(bool apply_for_system)
        {
            ulong tmp = tsru.GetSavedTimeStamp();
            if (apply_for_system)
            {
                ApplyOfflineTimeStamp();
            }
            while (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) try { Console.WriteLine("Network Connection Waiting...");slc.WriteLine("Network Connection Waiting..."); System.Threading.Thread.Sleep(2000); }catch(Exception e) { }


            ulong tmp2 = tsu.getTimeStampFromNetworks();
           
            if (tmp2 == 0) tmp2 = tmp;
            Console.WriteLine(BitConverter.ToString(BitConverter.GetBytes(tmp2)));
           
            slc.WriteLine(BitConverter.ToString(BitConverter.GetBytes(tmp2)));
           
            Console.WriteLine((tmp2).ToString());
            slc.WriteLine((tmp2).ToString());
            DateTime dt = tsu.ConvertFromUnixTimestamp(tmp2);
            SYSTEMTIME st = tsu.ConvertTimeStamp2SYSTEMTIME_struct(tmp2);
            
            if (apply_for_system&&!SetSystemTime(st)) Console.WriteLine("시스템 시간설정에 실패함");

            tsru.SaveTimeStamp(tmp2);
            Console.WriteLine(String.Format("System Time: {0}-{1}-{2}  {3}:{4}:{5}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute,dt.Second));
            slc.WriteLine(String.Format("System Time: {0}-{1}-{2}  {3}:{4}:{5}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute,dt.Second));
            TimeSpan timeOffset = DateTimeOffset.Now.Offset;
            dt = dt.AddSeconds(timeOffset.TotalSeconds);
            Console.WriteLine("btwn time: " + timeOffset.ToString());
            slc.WriteLine("btwn time: " + timeOffset.ToString());
            Console.WriteLine(String.Format("Local Time: {0}-{1}-{2}  {3}:{4}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute));
            slc.WriteLine(String.Format("Local Time: {0}-{1}-{2}  {3}:{4}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute));
        }
        Timer timer;
        protected override void OnStart(string[] args)
        {
            
            slc.Open(true);//OpenMessageQueue();
            //slc.
            Console.WriteLine("In OnStart.");
            slc.WriteLine("In OnStart.");
            
            ApplyTimeStamp(true);

            //Console.WriteLine((tmp2).ToString());
            timer = new Timer();
            timer.Interval = tsru.GetTimeUpdatePeriod()*1000; //15 * 1000; //tsru.GetTimeUpdatePeriod() * 1000; //30*60 * 1000; // 30*60 * 1000;
            slc.WriteLine(tsru.getLog());
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.AutoReset = true;
            timer.Start();
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

        }
        protected override void OnCustomCommand(int command)
        {
            Console.WriteLine("\tOnCustomCommand");
            slc.WriteLine("\tOnCustomCommand");
            if (command == (int)ServiceCommand.SERIVCECMD_RELOADHOSTLIST)
            {
                tsu.RequestReloadingHostsAndPorts();
                Console.WriteLine("\tOnCustomCommand::SERIVCECMD_RELOADHOSTLIST");
                slc.WriteLine("\tOnCustomCommand::SERIVCECMD_RELOADHOSTLIST");
                //tsu.RequestReloadingHostsAndPorts();
            }
            else if(command == (int)ServiceCommand.SERVICECMD_RELOAD_UPDATE_PRIOD)
            {
                timer.Stop();
                timer.Dispose();
                timer = new Timer();
                timer.Interval = tsru.GetTimeUpdatePeriod() * 1000;
                timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
                timer.AutoReset = true;
                timer.Start();
                Console.WriteLine("\tOnCustomCommand::SERVICECMD_RELOAD_UPDATE_PRIOD");
                slc.WriteLine("\tOnCustomCommand::SERVICECMD_RELOAD_UPDATE_PRIOD "+ tsru.GetTimeUpdatePeriod().ToString());
            }
        }
        private int eventId = 0;
        public void OnTimer(object sender,ElapsedEventArgs args)
        {
            Console.WriteLine("Monitoring the System" + (eventId++));
            slc.WriteLine("Monitoring the System" + eventId);
            ApplyTimeStamp(eventId%5==0);
            eventId = (eventId + 1) % 5;
            //eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId++);
        }

        protected override void OnStop()
        {
            //eventLog1.WriteEntry("In OnStop");
            Console.WriteLine("In OnStop");
            if (tm != null)
            {
                tm.Stop();
            }
            try
            {
                slc.WriteLine("In OnStop");
                slc.Close(true);
                slc.CloseWorker();
                slc.Close(true);
            }
            catch(Exception ex)
            {

            }
            //slc.CloseMessageQueue();
            //slc.CloseMem();
            tsru.onDestroy();
            tsu.onDestroy();
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 10 * 1000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            timer.Stop();
            
        }
        protected override void OnContinue()
        {
            base.OnContinue();

        }
    }
}
