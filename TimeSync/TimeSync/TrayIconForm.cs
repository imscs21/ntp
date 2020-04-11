using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TimeSync
{
    public partial class TrayIconForm : Form
    {
        public TrayIconForm()
        {
            InitializeComponent();
            notifyIcon1.ContextMenuStrip = this.contextMenuStrip2;
        }
    }
}
