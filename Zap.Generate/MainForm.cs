using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace Zap.Generate
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            var result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK 
                || result == System.Windows.Forms.DialogResult.Yes)
            { 
                //open file
                var filename = fileDialog.FileName;
                Assembly asm = null;
                try
                {
                    asm = Assembly.LoadFile(filename);
                }
                catch (Exception ee)
                {
                    MessageBox.Show("Not a valid .NET assembly.");
                }

                tbResult.Text = Zap.ProxyGenerator.Generate(asm);

            }
        }
    }
}
