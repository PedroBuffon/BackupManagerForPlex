using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PlexBackupApp
{
    public static class SSHRestoreUtilities
    {
        public static bool IsSSHAvailable()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "ssh";
                psi.Arguments = "-V";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit(5000);
                    return process.ExitCode == 0 || !string.IsNullOrEmpty(process.StandardError.ReadToEnd());
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSCPAvailable()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "scp";
                psi.Arguments = "";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit(2000);
                    return true; // SCP typically returns error when no args, but that means it's available
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetSSHRequirementsMessage()
        {
            var requirements = new List<string>();
            
            if (!IsSSHAvailable())
            {
                requirements.Add("• SSH client is not available. Please install OpenSSH client.");
            }
            
            if (!IsSCPAvailable())
            {
                requirements.Add("• SCP client is not available. Please install OpenSSH client.");
            }

            if (requirements.Count > 0)
            {
                return "Missing requirements for SSH restore:\n\n" + string.Join("\n", requirements) + 
                       "\n\nTo install OpenSSH on Windows:\n" +
                       "1. Open Settings > Apps > Optional Features\n" +
                       "2. Click 'Add a feature'\n" +
                       "3. Find and install 'OpenSSH Client'\n" +
                       "4. Restart the application";
            }

            return "";
        }
    }
}
