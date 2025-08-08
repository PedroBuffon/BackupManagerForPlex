using System;
using System.Collections.Generic;
using System.Diagnostics;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace PlexBackupApp
{
    public static class SSHRestoreUtilities
    {
        public static bool IsSSHAvailable()
        {
            // SSH.NET is available if the library is loaded
            // We can test this by trying to create a connection info object
            try
            {
                var connectionInfo = new ConnectionInfo("test", "test", new PasswordAuthenticationMethod("test", "test"));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSCPAvailable()
        {
            // SCP functionality is built into SSH.NET
            return IsSSHAvailable();
        }

        public static string GetSSHRequirementsMessage()
        {
            var requirements = new List<string>();
            
            if (!IsSSHAvailable())
            {
                requirements.Add("• SSH.NET library is not available or not properly installed.");
            }

            if (requirements.Count > 0)
            {
                return "Missing requirements for SSH restore:\n\n" + string.Join("\n", requirements) + 
                       "\n\nSSH.NET library should be automatically included with this application.\n" +
                       "If you see this message, please reinstall the application.";
            }

            return "";
        }

        public static bool TestSSHConnection(SSHConnectionConfig config, out string errorMessage)
        {
            errorMessage = "";
            
            try
            {
                AuthenticationMethod authMethod;
                
                if (config.UsePrivateKey && !string.IsNullOrEmpty(config.PrivateKeyPath))
                {
                    if (!System.IO.File.Exists(config.PrivateKeyPath))
                    {
                        errorMessage = "Private key file not found.";
                        return false;
                    }
                    
                    var keyFile = new PrivateKeyFile(config.PrivateKeyPath, config.Password);
                    authMethod = new PrivateKeyAuthenticationMethod(config.Username, keyFile);
                }
                else
                {
                    authMethod = new PasswordAuthenticationMethod(config.Username, config.Password);
                }

                var connectionInfo = new ConnectionInfo(config.HostIP, config.Port, config.Username, authMethod);
                
                using (var client = new SshClient(connectionInfo))
                {
                    client.Connect();
                    
                    if (client.IsConnected)
                    {
                        client.Disconnect();
                        return true;
                    }
                    else
                    {
                        errorMessage = "Failed to establish SSH connection.";
                        return false;
                    }
                }
            }
            catch (SshAuthenticationException ex)
            {
                errorMessage = $"Authentication failed: {ex.Message}";
                return false;
            }
            catch (SshConnectionException ex)
            {
                errorMessage = $"Connection failed: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"SSH connection error: {ex.Message}";
                return false;
            }
        }
    }
}
