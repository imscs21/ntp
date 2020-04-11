using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
namespace TimeSync
{
    static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        /// 
        static void ThRun()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyCustomApplicationContext());
        }
        static void Main()
        {
            //Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
            //Console.ReadLine();
             const bool interactive = true;
            const bool force_service = false;
             if (Environment.UserInteractive)
             {
                if (!force_service)
                {
                    Application.EnableVisualStyles();
                    Application.Run(new ControlForm1());
                }
                else
                {
                    new Service1().ForceRun(null);
                    Console.ReadLine();
                }
            }
             else
             {
                //new Service1().ForceRun(null);
                //Console.ReadLine();
                
                ServiceBase[] ServicesToRun;
                 ServicesToRun = new ServiceBase[]
                 {
                 new Service1()
                 };
                 Console.WriteLine(ServicesToRun[0].ServiceName);
                 ServiceBase.Run(ServicesToRun);
                
            }

        }
    }
    public class MyCustomApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        public MyCustomApplicationContext()
        {
            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon =  new Icon("Resources/Iynque_Ios7_Style_Clock.ico"),
                ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Exit", Exit)
            }),
                Visible = true
            };
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            //trayIcon.Visible = false;

            //Application.Exit();
        }
    }
}
