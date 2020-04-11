using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Threading;
using System.Windows.Forms.ComponentModel;
namespace TimeSync
{
    public enum FormStatus
    {
        Unknown=1,
        Showing=2,
        Hiding=3,
        Exiting=4
        
    }
    class PollWorkerWaiting
    {
        private Semaphore queue_sema,form_status_sema;
        private Queue<BackgroundWorker> poll_queue;
        private BackgroundWorker worker;
        private FormStatus form_status;
        public PollWorkerWaiting()
        {
            init();
        }
        private void init()
        {
            queue_sema = new Semaphore(1, 1);
            form_status_sema = new Semaphore(1, 1);
            poll_queue = new Queue<BackgroundWorker>();
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(Run);
        }
        public PollWorkerWaiting setFormStatus(FormStatus status)
        {
            form_status_sema.WaitOne();
            form_status = status;
            form_status_sema.Release();
            return this;
        }
        public FormStatus getFormStatus()
        {
            FormStatus fs = FormStatus.Unknown;
            form_status_sema.WaitOne();
            fs = form_status;
            form_status_sema.Release();
            return fs;
        }
        public void StartWork()
        {
            
            worker.RunWorkerAsync();
        }
        public void StopWork()
        {
            worker.CancelAsync();
        }
        public void Register(BackgroundWorker bw)
        {
            queue_sema.WaitOne();
            poll_queue.Enqueue(bw);
            queue_sema.Release();
        }
        
        protected void Run(object sender , DoWorkEventArgs arg)
        {
            BackgroundWorker w = sender as BackgroundWorker;
            while (!arg.Cancel)
            {
                if (w.CancellationPending)
                {
                    arg.Cancel = true;
                    break;
                }
                FormStatus fs = getFormStatus();
                if (fs == FormStatus.Showing)
                {
                    queue_sema.WaitOne();
                    if (poll_queue.Count > 0)
                    {
                        var tmp = poll_queue.Dequeue();
                        if (tmp.IsBusy)
                        {
                            poll_queue.Enqueue(tmp);
                        }
                        else
                        {
                            tmp.RunWorkerAsync();
                        }
                    }
                    queue_sema.Release();
                }
                else if (fs==FormStatus.Exiting)
                {
                    arg.Cancel = true;
                    break;
                }
                if (fs == FormStatus.Hiding)
                {
                    Thread.Sleep(1000);
                }
                else if (fs == FormStatus.Unknown)
                    Thread.Sleep(500);
                else
                    Thread.Sleep(1000 / 12);
            }
        }



    }
}
