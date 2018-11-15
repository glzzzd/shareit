using System;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace shareIt2
{
    public class Cmd
    {
        public static string exec(object command, bool ret)
        {
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                ProcessStartInfo procStartInfo =
                  new ProcessStartInfo("cmd", "/c " + command);

                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;
                // Now we create a process, assign its ProcessStartInfo and start it
                Process proc = new Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                // Get the output into a string
                string result = proc.StandardOutput.ReadToEnd();
                // Display the command output.
                byte[] bytes = Encoding.Default.GetBytes(result);
                result = Encoding.GetEncoding("cp866").GetString(bytes);
                if (ret)
                {
                    MessageBox.Show(result);
                    return result;
                }
                else {
                    return result;
                }
            }
            catch (Exception objException)
            {
                MessageBox.Show(objException.ToString());
            }
            return null;
        }
    }
}
