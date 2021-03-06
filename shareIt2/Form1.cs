﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32;

namespace shareIt2
{

    public partial class Form1 : Form
    {
        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        public Form1()
        {
            if (IsAdministrator() == false)
            {
                // Restart program and run as admin
                var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                startInfo.Verb = "runas";
                System.Diagnostics.Process.Start(startInfo);
                Application.Exit();
                return;
            }

            InitializeComponent();
            selectRights.SelectedIndex = 0;
            getShares();
            ServiceController sc = new ServiceController();
            sc.ServiceName = "lanmanserver";
            string st = sc.Status.ToString();
            if (st == "Running")
            {
                label4.Text = "running";
                label4.ForeColor = Color.FromArgb(0, 153, 0);
                button1.Text = "Stop LAN manager service";
            }
            else {
                label4.Text = "stopped";
                label4.ForeColor = Color.FromArgb(255,0,0);
                button1.Text = "Start LAN manager service";
            }
            
        }

        // Get list of shared folders
        public static List<string> GetSharedFolders()
        {

            List<string> sharedFolders = new List<string>();

            // Object to query the WMI Win32_Share API for shared files...

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from win32_share");

            //ManagementBaseObject outParams;

            ManagementClass mc = new ManagementClass("Win32_Share"); //for local shares

            foreach (ManagementObject share in searcher.Get())
            {

                string type = share["Type"].ToString();

                if (type == "0") // 0 = DiskDrive (1 = Print Queue, 2 = Device, 3 = IPH)
                {
                    string name = share["Name"].ToString(); //getting share name

                    string path = share["Path"].ToString(); //getting share path

                    string caption = share["Caption"].ToString(); //getting share description

                    sharedFolders.Add(path);
                }

            }

            return sharedFolders;

        }

        // Show list 
        public void getShares()
        {
            List<string> sharedFolders = GetSharedFolders();
            treeView1.Nodes.Clear();
            foreach (string value in sharedFolders)
            {
                TreeNode treeNode = new TreeNode(value);
                treeView1.Nodes.Add(treeNode);
            }
        }

        private void shareFolder_Click(object sender, EventArgs e)
        {
            
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            string path = fbd.SelectedPath;
            if (!string.IsNullOrEmpty(path))
            {
                string[] temp = path.Split('\\');
                string shareName = null;

                if (string.IsNullOrEmpty(temp[1]))
                {
                    string[] tmp = temp[0].Split(':');
                    shareName = "Drive " + tmp[0];
                    path = tmp[0] + ":";
                }
                else
                {
                    shareName = temp[temp.Length - 1];
                }
                //MessageBox.Show(path + "---" + shareName + "---" + temp.Length);

                int right = selectRights.SelectedIndex;
                string rights = null;
                string rights2 = null;
                string shareRes = null;
                string shareRes2 = null;

                if (right == 0)
                {
                    rights = "READ";
                    rights2 = "R";
                }
                else if (right == 1)
                {
                    rights = "FULL";
                    rights2 = "F";
                }

                if (!string.IsNullOrEmpty(path))
                {
                    string command1 = string.Format("net share \"{0}\"=\"{1}\" /grant:NETWORK,{2}", shareName, path, rights);
                    //MessageBox.Show(command1);
                    string command2 = string.Format("echo Y | CACLS \"{0}\" /E /G NETWORK:\"{1}\"", path, rights2);
                    //MessageBox.Show(command2);
                    shareRes = Cmd.exec(command1, false);
                    shareRes2 = Cmd.exec(command2, false);

                    try
                    {
                        if ((shareRes.IndexOf("successfully") == -1) || (string.IsNullOrEmpty(shareRes)))
                        {
                            MessageBox.Show("Cannot process directory. Perhaps, it is already shared.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            MessageBox.Show("Directory was successfully shared.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            getShares();
                        }
                        if (string.IsNullOrEmpty(shareRes2))
                        {
                            MessageBox.Show("Cannot grant access rights. Restart the application with administrator rights.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (NullReferenceException)
                    {
                        MessageBox.Show("Error.");
                    }

                }
            } //endif
        } //end of folder share


        /* tree view righ click */
        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeView1.SelectedNode = treeView1.GetNodeAt(e.X, e.Y);
                if (treeView1.SelectedNode != null && treeView1.SelectedNode.Parent == null)
                {
                    contextMenuStrip1.Show(treeView1, e.Location);
                }
            }
        }

        /* Double click tree view item */
        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            string t = node.Text;
            Process.Start(t);
        }

        private void unshare_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            string shareName = node.Text;
            string diskName = node.Text;
            string[] temp = shareName.Split('\\');

            //MessageBox.Show(temp.ToString());
            if (string.IsNullOrEmpty(temp[1]))
            {
                string[] tmp = temp[0].Split(':');
                shareName = "Drive "+tmp[0];
                diskName = tmp[0] + ":";
            }

            object c1 = string.Format("net share \"{0}\" /delete /y", shareName);
            object c2 = string.Format("echo Y | CACLS \"{0}\" /E /R Network",diskName);
            string shRes = null;
            string shRes2 = null;
            try
            {
                shRes = Cmd.exec(c1, false);
                shRes2 = Cmd.exec(c2, false);
                node.Remove();
            }
            catch (Exception)
            {
                MessageBox.Show("Error.");
            }


            /* Error handling */
            if (shRes.IndexOf("successfully") == -1)
            {
                MessageBox.Show("Cannot remove the share.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (shRes.IndexOf("error 5") != -1) {
                MessageBox.Show("Directory is already in use. Please stop LAN manager service", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (shRes2.IndexOf("processed") == -1)
            {
                MessageBox.Show("Cannot remove access rights.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("Direcory was processed successfully.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /* File and printer sharing settings */
        private void system_Click(object sender, EventArgs e)
        {
            string r1 = Cmd.exec("netsh advfirewall firewall set rule group=\"File and printer sharing\" new enable=Yes", false);
            string r2 = Cmd.exec("netsh advfirewall firewall set rule group=\"Network discovery\" new enable=Yes", false);
            string r3 = Cmd.exec("net user guest /active:yes", false);

            string osVersion = OSVersion.getOSInfo();
                Process compiler = new Process();
                compiler.StartInfo.FileName = "lib\\SetACL32.exe";
                compiler.StartInfo.Arguments = "-on HKEY_LOCAL_MACHINE\\SECURITY\\ -ot reg -actn setowner -ownr n:Administrators";
                compiler.StartInfo.UseShellExecute = false;
                compiler.StartInfo.RedirectStandardOutput = true;
                compiler.Start();
                string r4 = compiler.StandardOutput.ReadToEnd();

                Process compiler2 = new Process();
                compiler2.StartInfo.FileName = "lib\\SetACL32.exe";
                compiler2.StartInfo.Arguments = "-on HKEY_LOCAL_MACHINE\\SECURITY\\ -ot reg -actn ace -ace \"n:Administrators;p:full\"";
                compiler2.StartInfo.UseShellExecute = false;
                compiler2.StartInfo.RedirectStandardOutput = true;
                compiler2.Start();
                string r5 = compiler2.StandardOutput.ReadToEnd();

                NTAccount f = new NTAccount("Guest");
                SecurityIdentifier s = (SecurityIdentifier)f.Translate(typeof(SecurityIdentifier));
                String sid = s.ToString();

                string rightsRes = Cmd.exec(string.Format("reg add HKLM\\SECURITY\\Policy\\Accounts\\{0}\\ActSysAc /ve /t reg_binary /d 41000000 /f", sid), false);

            /* Обработка ошибок */
            if (rightsRes.IndexOf("successfully") == -1)
            {
                MessageBox.Show("Cannot update registry.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (r4.IndexOf("success") == -1)
            {
                MessageBox.Show("Cannot update registry access rights.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (r5.IndexOf("success") == -1)
            {
                MessageBox.Show("Cannot update registry access rights.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if(r1.IndexOf("Updated") == -1){
                MessageBox.Show("Cannot share files and printers.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (r2.IndexOf("Updated") == -1)
            {
                MessageBox.Show("Cannot turn on network discovery.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (r3.IndexOf("successfully") == -1)
            {
                MessageBox.Show("Cannot turn guest account.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                DialogResult rest = MessageBox.Show("Setting were applied. You need to restart your PC. Restart now?", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (rest == DialogResult.Yes)
                {
                   Cmd.exec("shutdown /r /t 0",false);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            ServiceController sc = new ServiceController();
            sc.ServiceName = "lanmanserver";
            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                // Start the service if the current status is stopped.
                //MessageBox.Show("Starting the Alerter service...");
                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);
                    label4.Text = "runnnig";
                    label4.ForeColor = Color.FromArgb(0, 153, 0);
                    button1.Text = "Stop LAN manager service";
                    getShares();
                    button1.Enabled = true;
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("Cannot stop LAN manager service.");
                    button1.Enabled = true;
                }
            }
            else {
                try
                {
                    button1.Enabled = false;
                    // Start the service, and wait until its status is "Running".
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                    label4.Text = "stopped";
                    label4.ForeColor = Color.FromArgb(255, 0, 0);
                    button1.Text = "Start LAN manager service";
                    button1.Enabled = true;
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("Cannot start LAN manager service.");
                    button1.Enabled = true;
                }
            }
        }

        /* TOP MENU */
        private void eixtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void посетитьСайтПрограммыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://github.com/glzzzd/shareIt/");
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot open browser.");
            }
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("shareIt v. 0.3.0\r\nIlya Hlazdouski, 2016\r\nMIT License");
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        /* Change PC name */
        private bool SetMachineName(string newName)
        {
            RegistryKey key = Registry.LocalMachine;

            string activeComputerName = "SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ActiveComputerName";
            RegistryKey activeCmpName = key.CreateSubKey(activeComputerName);
            activeCmpName.SetValue("ComputerName", newName);
            activeCmpName.Close();
            string computerName = "SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName";
            RegistryKey cmpName = key.CreateSubKey(computerName);
            cmpName.SetValue("ComputerName", newName);
            cmpName.Close();
            string _hostName = "SYSTEM\\CurrentControlSet\\services\\Tcpip\\Parameters\\";
            RegistryKey hostName = key.CreateSubKey(_hostName);
            hostName.SetValue("Hostname", newName);
            hostName.SetValue("NV Hostname", newName);
            hostName.Close();
            return true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string name = textBox1.Text;
            if (this.SetMachineName(name))
            {
                DialogResult rest = MessageBox.Show("Setting were applied. You need to restart your PC. Restart now?", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (rest == DialogResult.Yes)
                {
                    Cmd.exec("shutdown /r /t 0", false);
                }
            }
            else
            {
                MessageBox.Show("Cannot rename PC.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void выйтиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
