using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Data.SQLite;
using System.ServiceProcess;

namespace TimeSync
{
    public partial class ControlForm1 : Form
    {
        private Service1 mService;
        NTPServersManager srvmgr;
        TimeStampRegistryUtil tsru;
        BackgroundWorker service_status_progress_updater,service_status_confirmation;
        private bool form_close, app_exit;
        private BackgroundWorker service_log_poll_worker;
        private ServiceControllerPermission scp = null;
        private ServiceLogController slc;
        private PollWorkerWaiting pww;
        public ControlForm1()
        {
            //mService = service;
            srvmgr = new NTPServersManager();
            tsru = TimeStampRegistryUtil.getInstance();
            InitializeComponent();
            slc = ServiceLogController.getInstance();
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            pww = new PollWorkerWaiting();
            /*
            this.Hide();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;*/
            HideForm();
            app_exit = false;
            form_close = true;
            service_log_poll_worker = new BackgroundWorker();
            service_log_poll_worker.WorkerReportsProgress = false;
            service_log_poll_worker.WorkerSupportsCancellation = true;
            service_log_poll_worker.DoWork += new DoWorkEventHandler(pollworker);
            //service_log_poll_worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            //service_log_poll_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            service_status_progress_updater = new BackgroundWorker();
            service_status_progress_updater.WorkerReportsProgress = true;
            service_status_progress_updater.WorkerSupportsCancellation = true;
            service_status_progress_updater.DoWork += new DoWorkEventHandler(ServiceStatusUpdate);
            service_status_progress_updater.ProgressChanged += new ProgressChangedEventHandler(ServiceStatus_ProgressUpdated);
            service_status_confirmation = new BackgroundWorker();
            service_status_confirmation.WorkerReportsProgress = true;
            service_status_confirmation.WorkerSupportsCancellation = true;
            service_status_confirmation.DoWork += new DoWorkEventHandler(ServiceStatusProgressUpdate);
            service_status_confirmation.ProgressChanged += new ProgressChangedEventHandler(ServiceStatusProgressUpdate_ProgressUpdated);
           
        }
        private void ServiceStatusProgressUpdate(object sender,DoWorkEventArgs e)
        {
            int predicted_time = 0;
            const int limit = 30 * 1000;
            const int fps = 1000 / 25;
            try
            {
                for (int i = 1; i <= limit&&!e.Cancel; i++)
                {
                    if (service_status_confirmation.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    if (i % fps == 0)
                    {
                        service_status_confirmation.ReportProgress((int)(((double)i + 0.0) / ((double)limit + 0.0) * 100.0));
                    }
                        System.Threading.Thread.Sleep(1);
                }
            }catch(Exception ex)
            {

            }
        }
        private void ServiceStatusProgressUpdate_ProgressUpdated(object sender,ProgressChangedEventArgs e)
        {
           int value = e.ProgressPercentage;
            if (toolStripProgressBar1.GetCurrentParent().InvokeRequired)
                toolStripProgressBar1.GetCurrentParent().Invoke(new System.Windows.Forms.MethodInvoker(() => { toolStripProgressBar1.Value = value; }));
            else

                toolStripProgressBar1.Value = value;
        }
        private void ServiceStatusUpdate(object sender, DoWorkEventArgs e)
        {
            try
            {
                service_status_confirmation.CancelAsync();
            }
            catch(Exception ex)
            {

            }
            service_status_confirmation.RunWorkerAsync();
            System.ServiceProcess.ServiceControllerStatus aim = (ServiceControllerStatus)e.Argument;
            try
            {
                serviceController1.WaitForStatus(aim, TimeSpan.FromSeconds(30));
                service_status_confirmation.CancelAsync();
                service_status_progress_updater.ReportProgress(100);
            }
            catch(Exception ez)
            {
                service_status_confirmation.CancelAsync();
                service_status_progress_updater.ReportProgress(0);
            }
           


        }
        private void ServiceStatus_ProgressUpdated(object sender,ProgressChangedEventArgs e)
        {
            int value = e.ProgressPercentage;
            if (toolStripProgressBar1.GetCurrentParent().InvokeRequired)
                toolStripProgressBar1.GetCurrentParent().Invoke(new System.Windows.Forms.MethodInvoker(() => { toolStripProgressBar1.Value = value; }));
            else

                toolStripProgressBar1.Value = value;
            //toolStripProgressBar1.Value = value;
            if (toolStripStatusLabel1.GetCurrentParent().InvokeRequired)
                toolStripStatusLabel1.GetCurrentParent().Invoke(new System.Windows.Forms.MethodInvoker(() => { ServiceStatusDisplayUpdate(); }));
            else
                
                ServiceStatusDisplayUpdate();
        }
        private void pollworker(object sender, DoWorkEventArgs e)
        {
            slc.Open(false);
            slc.ClientConnect();
            //byte[] bs = new byte[8192*4];
            while (!e.Cancel&&!form_close && !app_exit)
            {
               
                if (service_log_poll_worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                if (!slc.IsClientConnected())
                {
                    try
                    {
                        slc.ClientConnect(50);
                    }
                    catch (ObjectDisposedException ode)
                    {
                        try
                        {
                            slc.Close(false);
                        }
                        catch (Exception ex)
                        {
                           
                        }
                        slc.Open(false);
                    }
                    catch (Exception ex)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                    continue;
                }
                //slc.Read(bs);
                try
                {
                    String bs = slc.ReadLine();
                    if (bs != null)
                    {
                        String str = bs.Trim('\0').Trim(); //Encoding.ASCII.GetString(bs).Trim('\0').Trim();
                        String txt = str + "\n"; //String.Format("data{0}:'{1}", str.Length, str+"'");
                        if (richTextBox1.InvokeRequired)
                            richTextBox1.Invoke(new System.Windows.Forms.MethodInvoker(() => { richTextBox1.Text += txt; }));
                        else
                            richTextBox1.Text += txt;
                    }
                    if (service_log_poll_worker.CancellationPending)
                    {
                        e.Cancel = true;
                        //break;
                        continue;
                    }
                    try
                    {
                        System.Threading.Thread.Sleep(250);
                    }
                    catch (Exception ex)
                    {

                    }
                }catch(Exception protector)
                {
                    break;
                }
            }
            slc.Close(false);

        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            serviceController1.Start();
            ServiceStatusDisplayUpdate();
            service_status_progress_updater.RunWorkerAsync(System.ServiceProcess.ServiceControllerStatus.Running);
        }

        private void exitAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var resp  = MessageBox.Show("모든 시간동기화 애플리케이션을 종료하시겠습니까?", "의사확인",MessageBoxButtons.YesNo);
            //this.
            if (resp == DialogResult.Yes)
            {
                if (serviceController1.Status != ServiceControllerStatus.Stopped)
                {
                    serviceController1.Stop();
                }
                AppExit();
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Show();
            */
            ShowForm();
        }

        private void ControlForm1_Load(object sender, EventArgs e)
        {
            dataGridView1.DataSource = srvmgr.getHostsGridData();
            dataGridView2.DataSource = srvmgr.getPortsGridData();
            numericUpDown1.Value = tsru.GetTimeUpdatePeriod();
            pww.StartWork();
            //slc.Open(false);
            //slc.OpenMessageQueue();
            //slc.OpenMemAsRO();
        }

        private void ControlForm1_Resize(object sender, EventArgs e)
        {
           
        }
        
        private void HideForm()
        {
            pww.setFormStatus(FormStatus.Hiding);
            try
            {
                if (service_log_poll_worker != null)
                    service_log_poll_worker.CancelAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            this.Hide();
            form_close = true;
            
            //this.WindowState = FormWindowState.Minimized;
            //this.ShowInTaskbar = false;
        }
        private void ShowForm()
        {
            this.Show();

            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            form_close = false;
            ServiceStatusDisplayUpdate();
            //Console.WriteLine("poll worker is busy::" + service_log_poll_worker.IsBusy.ToString());
            pww.setFormStatus(FormStatus.Showing);
            pww.Register(service_log_poll_worker);
           
            //if(service_log_poll_worker.IsBusy!=true)
            //service_log_poll_worker.RunWorkerAsync();
        }
        private void ControlForm1_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideForm();
                
            }
            
        }
        private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            //InitializeComponent();
            if (e.ColumnIndex == 0)
            {
                dataGridView1.CancelEdit();
            }
        }
        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataTable dt = ((DataTable)dataGridView1.DataSource);
            string id = dt.Rows[e.RowIndex]["id"] + "";
            string col = dt.Columns[e.ColumnIndex].ColumnName;
            string data = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value + "";

            string sql = string.Format("UPDATE hosts SET `{0}` = '{1}' WHERE id = {2};", col, data, id);
            srvmgr.UpdateInsertDeleteData(sql);

        }
        private void dataGridView2_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            //InitializeComponent();
            if (e.ColumnIndex == 0)
            {
                dataGridView1.CancelEdit();
            }
        }
        private void dataGridView2_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataTable dt = ((DataTable)dataGridView1.DataSource);
            //dataGridView1.Ins
            string col = dt.Columns[e.ColumnIndex].ColumnName;
            string data = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value + "";

            string sql = string.Format("UPDATE ports SET `{0}` = '{1}';", col, data);
            srvmgr.UpdateInsertDeleteData(sql);
        }
        private void ServiceStatusDisplayUpdate()
        {

            if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.Running)
            {
                toolStripStatusLabel1.Text = "시작됨";
            }
            else if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.Stopped)
            {
                toolStripStatusLabel1.Text = "중지됨";
            }
            else if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.StartPending)
            {
                toolStripStatusLabel1.Text = "시작중...";
            }
            else if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.StopPending)
            {
                toolStripStatusLabel1.Text = "중지중...";
            }
            else if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.Paused)
            {
                toolStripStatusLabel1.Text = "일시정지됨";
            }

        }
        private void ControlForm1_Shown(object sender, EventArgs e)
        {
            if(scp==null)
            scp = new ServiceControllerPermission(ServiceControllerPermissionAccess.Control, Environment.MachineName, "Service1");//this will grant permission to access the Service
            //slc.ClientConnect();
            //service_log_poll_worker.RunWorkerAsync();
            scp.Assert();
            serviceController1.Refresh();
            label1.Text = serviceController1.DisplayName + " 서비스 ";
            
        }

        private void dataGridView1_VisibleChanged(object sender, EventArgs e)
        {
            

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //new MessageBox()
            List<String> rows = new List<String>();
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                rows.Add(String.Format("{0}행(id={1})", row.Cells[0].RowIndex+1,row.Cells[0].Value));
            }
            if (rows.Count==0)
            {
                MessageBox.Show("어떤행도 제대로 선택되지 않았습니다", "삭제불가");
                return;
            }
                DialogResult result1 = MessageBox.Show(String.Join(" , ",rows)+"을 삭제하시겠습니까?", "의사확인",
                                                   MessageBoxButtons.YesNo);
            if (result1 == DialogResult.No) return;
            SQLiteConnection conn = srvmgr.getSingleConnection();
            SQLiteCommand cmd = new SQLiteCommand(conn);
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                    {
                        //get key
                        int rowId = Convert.ToInt32(row.Cells[0].Value);

                        //avoid updating the last empty row in datagrid
                        if (rowId > 0)
                        {
                            //delete 
                            //aController.Delete(rowId);
                            cmd.CommandText = string.Format("DELETE FROM hosts Where id={0}", rowId);
                            cmd.ExecuteNonQuery();
                            //refresh datagrid
                            dataGridView1.Rows.RemoveAt(row.Index);
                        }
                    }
                    tran.Commit();
                }catch(Exception ex)
                {
                    tran.Rollback();
                }
            }
            
            srvmgr.CloseSingle();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            List<String> rows = new List<String>();
            foreach (DataGridViewRow row in dataGridView2.SelectedRows)
            {
                rows.Add(String.Format("{0}행(port={1})", row.Cells[0].RowIndex + 1, row.Cells[0].Value));
            }
            if (rows.Count == 0)
            {
                MessageBox.Show("어떤행도 제대로 선택되지 않았습니다", "삭제불가");
                return;
            }
            DialogResult result1 = MessageBox.Show(String.Join(" , ", rows) + "을 삭제하시겠습니까?", "의사확인",
                                               MessageBoxButtons.YesNo);
            if (result1 == DialogResult.No) return;
            SQLiteConnection conn = srvmgr.getSingleConnection();
            SQLiteCommand cmd = new SQLiteCommand(conn);
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    foreach (DataGridViewRow row in dataGridView2.SelectedRows)
                    {
                        //get key
                        int rowId = Convert.ToInt32(row.Cells[0].Value);

                        //avoid updating the last empty row in datagrid
                        if (rowId > 0)
                        {
                            //delete 
                            //aController.Delete(rowId);
                            cmd.CommandText = string.Format("DELETE FROM ports Where port={0}", rowId);
                            cmd.ExecuteNonQuery();
                            //refresh datagrid
                            dataGridView2.Rows.RemoveAt(row.Index);
                        }
                    }
                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                }
            }

            srvmgr.CloseSingle();
        }

        private void dataGridView2_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            DataGridViewRow row = e.Row;
            string sql = string.Format("insert into ports values({0})", Convert.ToInt64(row.Cells[0].Value));
            srvmgr.UpdateInsertDeleteData(sql);
            
        }

        private void dataGridView1_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            DataGridViewRow row = e.Row;
            string sql = string.Format("insert into hosts(host,is_default) values('{0}',{1})",row.Cells[1].Value, Convert.ToInt64(row.Cells[2].Value));
            srvmgr.UpdateInsertDeleteData(sql);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            uint val = Convert.ToUInt32(numericUpDown1.Value);
            if (tsru.SetTimeUpdatePeriod(val))
            {
                String msg = "성공적으로 수정되었습니다";
                try
                {
                    serviceController1.ExecuteCommand((int)TimeSync.ServiceCommand.SERVICECMD_RELOAD_UPDATE_PRIOD);
                }catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    msg = "성공적으로 적용되었지만 실행중인 서비스에는 반영되지 못했습니다";
                }
                MessageBox.Show(msg, "적용됨");
            }
            else
            {
                MessageBox.Show("적용에 실패했습니다");
            }

            
        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

            slc.Close(false);
            serviceController1.Stop();

            //serviceController1.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(10 * 1000));
            ServiceStatusDisplayUpdate();
            service_status_progress_updater.RunWorkerAsync(System.ServiceProcess.ServiceControllerStatus.Stopped);
        }
        private void AppExit()
        {
            pww.setFormStatus(TimeSync.FormStatus.Exiting);
            pww.StopWork();
            try
            {
                slc.Close(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Application.Exit();
            app_exit = true;
        }
        private void exitTrayUIAppToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AppExit();
        }

        private void ControlForm1_FormClosed(object sender, FormClosedEventArgs e)
        {
            
            //slc.CloseMessageQueue();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            serviceController1.ExecuteCommand((int)TimeSync.ServiceCommand.SERVICECMD_RELOAD_UPDATE_PRIOD);
        }

        
        private void RefreshMenuStripService()
        {
            if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.Running)
            {
                bool is_start_mode = true;
                startToolStripMenuItem.Enabled = !is_start_mode;
                restartToolStripMenuItem.Enabled = is_start_mode;
                stopToolStripMenuItem.Enabled = is_start_mode;
            }
            else if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.Stopped)
            {
                bool is_start_mode = false;
                startToolStripMenuItem.Enabled = !is_start_mode;
                restartToolStripMenuItem.Enabled = is_start_mode;
                stopToolStripMenuItem.Enabled = is_start_mode;
            }
            else if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.StartPending)
            {
                bool is_start_mode = true;
                startToolStripMenuItem.Enabled = !is_start_mode;
                restartToolStripMenuItem.Enabled = is_start_mode;
                stopToolStripMenuItem.Enabled = is_start_mode;
            }
            else if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.StopPending)
            {
                bool is_start_mode = false;
                startToolStripMenuItem.Enabled = !is_start_mode;
                restartToolStripMenuItem.Enabled = is_start_mode;
                stopToolStripMenuItem.Enabled = is_start_mode;
            }
            else if (serviceController1.Status == System.ServiceProcess.ServiceControllerStatus.Paused)
            {
                //bool is_start_mode = false;
                startToolStripMenuItem.Enabled = true; //!is_start_mode;
                restartToolStripMenuItem.Enabled = true; //is_start_mode;
                stopToolStripMenuItem.Enabled = true; //is_start_mode;
            }
        }
        private void contextMenuStrip1_VisibleChanged(object sender, EventArgs e)
        {
            Console.WriteLine("contextMenuStrip1_VisibleChanged");
            //if (contextMenuStrip1.Visible) 
                RefreshMenuStripService();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startToolStripMenuItem.Enabled = false;

            serviceController1.Start();
            serviceController1.WaitForStatus(ServiceControllerStatus.StartPending, TimeSpan.FromMilliseconds(700));
            RefreshMenuStripService();
            serviceController1.WaitForStatus(ServiceControllerStatus.Running);
            RefreshMenuStripService();
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            restartToolStripMenuItem.Enabled = false;
            serviceController1.Stop();

            serviceController1.WaitForStatus(ServiceControllerStatus.StopPending, TimeSpan.FromMilliseconds(700));
            RefreshMenuStripService();
            serviceController1.WaitForStatus(ServiceControllerStatus.Stopped);
            RefreshMenuStripService();
            serviceController1.Start();
            serviceController1.WaitForStatus(ServiceControllerStatus.StartPending, TimeSpan.FromMilliseconds(700));
            RefreshMenuStripService();
            serviceController1.WaitForStatus(ServiceControllerStatus.Running);
            RefreshMenuStripService();
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopToolStripMenuItem.Enabled = false;
            slc.Close(false);

            serviceController1.Stop();
           
            serviceController1.WaitForStatus(ServiceControllerStatus.StopPending, TimeSpan.FromMilliseconds(700));
            RefreshMenuStripService();
            serviceController1.WaitForStatus(ServiceControllerStatus.Stopped);
            RefreshMenuStripService();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            serviceController1.ExecuteCommand((int)TimeSync.ServiceCommand.SERIVCECMD_RELOADHOSTLIST);
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            //const MouseButtons Which_Btn = System.Windows.SystemParameters.SwapButtons == true ? MouseButtons.Right : MouseButtons.Left;//did not work
            if (System.Windows.SystemParameters.SwapButtons)
            {
                if (e.Button == MouseButtons.Right)
                {
                    ShowForm();
                }
            }
            else
            {
                if(e.Button == MouseButtons.Left)
                {
                    ShowForm();
                }
                else
                {
                    
                }
            }
        }

        
    }
}
