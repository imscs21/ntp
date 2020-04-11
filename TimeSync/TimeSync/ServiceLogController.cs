using System;
using System.Collections.Generic;
//using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Messaging;
using System.ComponentModel;
using System.Collections;

namespace TimeSync
{
    class ServiceLogController
    {
        private static ServiceLogController mInstance = null;
        public static ServiceLogController getInstance()
        {
            if (mInstance == null) mInstance = new ServiceLogController();
            return mInstance;
        }

        protected ServiceLogController()
        {
            init();
        }
        Semaphore rw_semaphore,data_queue_semaphore;
        private void init()
        {
            file = null;
            view = null;
            service_exited = false;
            rw_semaphore = new Semaphore(1, 1);
            data_queue_semaphore = new Semaphore(1, 1);
            size = 2048; //8192 * 16;
            exit_app = false;

        }
        [StructLayout(LayoutKind.Sequential)]
        internal class SecurityAttributes
        {
            public SecurityAttributes(object securityDescriptor)
            {
                this.lpSecurityDescriptor = securityDescriptor;
            }
            uint nLength = 12;
            object lpSecurityDescriptor;
            [MarshalAs(UnmanagedType.VariantBool)]
            bool bInheritHandle = true;
        }
        [DllImport("Kernel32.dll", EntryPoint = "CreateFileMapping", SetLastError = true, CharSet = CharSet.Unicode)]
        protected internal static extern IntPtr CreateFileMapping(uint hFile, SecurityAttributes lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
        [DllImport("Kernel32.dll", EntryPoint = "OpenFileMapping", SetLastError = true, CharSet = CharSet.Unicode)]
        protected internal static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);
        [DllImport("Kernel32.dll", EntryPoint = "MapViewOfFile", SetLastError = true, CharSet = CharSet.Unicode)]
        protected internal static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
        [DllImport("Kernel32.dll", EntryPoint = "UnmapViewOfFile", SetLastError = true, CharSet = CharSet.Unicode)]
        protected internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);
        [DllImport("Kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true, CharSet = CharSet.Unicode)]
        protected internal static extern bool CloseHandler(uint hHandle);
        [DllImport("Kernel32.dll", EntryPoint = "GetLastError", SetLastError = true, CharSet = CharSet.Unicode)]
        protected internal static extern uint GetLastError();
        private MemoryMappedFile file;
        private MemoryMappedViewStream view;
        private static String SM_NAME = "Global\\hshTimeSyncLog";
        private static String Q_NAME = ".\\hshTimeSyncLogQ";
        private static String S_NAME = "hshTimeSyncLogP";
        private NamedPipeServerStream srv_pipe;
        private NamedPipeClientStream cli_pipe;
        BackgroundWorker wait_worker;
        private int size = 0;
        private void WaitWorkerInitialize()
        {
            Console.WriteLine("[WaitWorkerInitialize]");
            data_queue = new Queue<string>();
            wait_worker = new BackgroundWorker();
            wait_worker.WorkerReportsProgress = true;
            wait_worker.WorkerSupportsCancellation = true;
            wait_worker.ProgressChanged+= new ProgressChangedEventHandler(wait_worker_notifiy);
            wait_worker.DoWork += new DoWorkEventHandler(wait_worker_work);
        }
        private bool service_exited;
        public bool IsServiceExited()
        {
            lock (this)
            {
                return service_exited;
            }
        }
        public void SetServiceExited(bool state)
        {
            lock (this)
            {
                service_exited = state;
            }
        }
        protected void wait_worker_work(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("[[wait_worker_work]]started");
            while (!e.Cancel&&!IsServiceExited())
            {
                if (wait_worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                try
                {
                    if (!srv_pipe.IsConnected)
                    {
                        //srv_pipe.RunAsClient
                        srv_pipe.WaitForConnection();
                        //srv_pipe.WaitForConnection()
                        Console.WriteLine("pip connected");
                        wait_worker.ReportProgress(1);
                    }
                }catch(IOException ex)
                {
                    //sw.Dispose();
                    srv_pipe.Dispose();

                    srv_pipe = new NamedPipeServerStream(S_NAME, PipeDirection.Out);
                    sw = new StreamWriter(srv_pipe);
                    Console.WriteLine(ex);
                }
                Thread.Sleep(10 * 1000);
            }
        }
        Queue<String> data_queue;
        protected void wait_worker_notifiy(object sender, ProgressChangedEventArgs e)
        {
            Console.WriteLine("wait_worker_notifiy");
            if (srv_pipe.IsConnected)
            {
                data_queue_semaphore.WaitOne();
                Console.WriteLine("wait_worker_notifiy => flush queue");
                while (data_queue.Count > 0)
                {
                    Write(data_queue.Dequeue());
                }
                sw.Flush();
                data_queue_semaphore.Release();
                Console.WriteLine("finish flush queue");
            }
        }
        public void Open(bool is_server)
        {
            if (is_server)
            {
                try
                {
                    srv_pipe = new NamedPipeServerStream(S_NAME, PipeDirection.Out);
                    //srv_pipe.WaitForConnection();
                    WaitWorkerInitialize();
                    wait_worker.RunWorkerAsync();
                }catch(Exception e) { }
            }
            else
            {
                cli_pipe = new NamedPipeClientStream(".",S_NAME,PipeDirection.In);
                sr = new StreamReader(cli_pipe);
                //cli_pipe.Connect();
            }
        }
        public bool IsClientConnected()
        {
            return cli_pipe!=null&&cli_pipe.IsConnected;
        }
        public void ClientConnect()
        {
            //Console.WriteLine("Start ClientConnect");
            cli_pipe.Connect();
            //Console.WriteLine("End ClientConnect " + cli_pipe.IsConnected.ToString());
        }
        public void ClientConnect(int timeout_milli)
        {
            //Console.WriteLine("Start ClientConnect");
            cli_pipe.Connect(timeout_milli);
            //Console.WriteLine("End ClientConnect " + cli_pipe.IsConnected.ToString());
        }
        public void CloseWorker()
        {
            if (srv_pipe != null)
            {
                wait_worker.CancelAsync();
                wait_worker.Dispose();
            }
        }
        public void Close(bool is_server)
        {
            if (is_server)
            {
                try
                {
                    srv_pipe.Disconnect();
                }
                catch(Exception e) { }
                try
                {
                    sw.Dispose();
                    srv_pipe.Close();
                    srv_pipe.Dispose();
                }catch(Exception e) { }
            }
            else
            {

                //cli_pipe.Close();
                try
                {
                    cli_pipe.Dispose();
                }catch(Exception e)
                {

                }
            }
        }
        private bool exit_app = false;
        
        public void Write(byte[] rb)
        {
            //view.Write()

            rw_semaphore.WaitOne();
            if (exit_app == true)
            {
                rw_semaphore.Release();
                return;
            }
            else if (srv_pipe == null) Open(false);
            srv_pipe.Write(rb, 0, rb.Length);
            rw_semaphore.Release();
        }
        StreamWriter sw=null;
        public void Write(String str)
        {
            try
            {
                rw_semaphore.WaitOne();
                if (exit_app)
                {
                    rw_semaphore.Release();
                    return;
                }
                else if (srv_pipe == null) return;
                if (srv_pipe.IsConnected)
                {
                    try
                    {
                        if (sw == null) sw = new StreamWriter(srv_pipe);
                        sw.Write((String)str);

                        //sw.Flush();
                        //rw_semaphore.Release();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        data_queue.Enqueue(str + "\n");
                        // rw_semaphore.Release();
                    }
                }
                else
                {
                    data_queue_semaphore.WaitOne();

                    data_queue.Enqueue(str);
                    while (data_queue.Count > 8192)
                    {
                        data_queue.Dequeue();
                    }
                    data_queue_semaphore.Release();
                    //rw_semaphore.Release();
                }
                rw_semaphore.Release();
            }catch(Exception except)
            {
                rw_semaphore.Release();
            }
        }
        public void WriteLine(String str)
        {
            try
            {
                rw_semaphore.WaitOne();
                Console.WriteLine("SLC::WriteLine");
                if (exit_app)
                {
                    rw_semaphore.Release();
                    return;
                }
                else if (srv_pipe == null) return;
                if (srv_pipe.IsConnected)
                {
                    try
                    {
                        //data_queue_semaphore.WaitOne();
                        Console.WriteLine("normal mode");
                        if (sw == null) sw = new StreamWriter(srv_pipe);
                        sw.WriteLine(str);
                        //rw_semaphore.Release();
                        sw.Flush();

                        //data_queue_semaphore.Release();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("imergency record mode");
                        Console.WriteLine(ex);
                        //data_queue_semaphore.WaitOne();
                        data_queue.Enqueue(str + "\n");
                        //rw_semaphore.Release();
                        //data_queue_semaphore.Release();
                    }
                }
                else
                {
                    Console.WriteLine("record mode");
                    data_queue_semaphore.WaitOne();
                    data_queue.Enqueue(str + "\n");
                    while (data_queue.Count > 8192)
                    {
                        data_queue.Dequeue();
                    }
                    data_queue_semaphore.Release();
                    //rw_semaphore.Release();
                }
                rw_semaphore.Release();
            }catch(Exception except)
            {

                rw_semaphore.Release();
            }
            // Write(str + "\n");
        }
        StreamReader sr;
        public String ReadLine()
        {
            rw_semaphore.WaitOne();
            if (exit_app)
            {
                //Console.WriteLine("ReadLine exit_app=true"); 
                rw_semaphore.Release();
                return null;
            }
            else if (cli_pipe == null) Open(false);
            //else if (mq == null) OpenMessageQueue();
            if (sr == null) sr = new StreamReader(cli_pipe);
            try
            {
                String rst = null;
               
                rst = sr.ReadLine();

                return rst;
            }catch(ObjectDisposedException mqe)
            {
                Console.WriteLine(mqe.ToString());
                return null;
            }
            finally
            {
                rw_semaphore.Release();
            }
        }
        public byte[] Read(byte[] rb)
        {
            rw_semaphore.WaitOne();
            if (exit_app)
            {

                rw_semaphore.Release();
                return null;
            }
            else if (cli_pipe == null) Open(false);
            //else if (mq == null) OpenMessageQueue();
            try
            {
                cli_pipe.Read(rb, 0, rb.Length);
                //Message msg = mq.Receive(TimeSpan.FromSeconds(2), MessageQueueTransactionType.Automatic);
                //byte[] rst = (byte[])msg.Body;
                return rb;
            }catch(MessageQueueException mqe)
            {
                Console.WriteLine(mqe.ToString());
                return null;
            }
            finally
            {
                rw_semaphore.Release();
            }
            
        }
    }
}
