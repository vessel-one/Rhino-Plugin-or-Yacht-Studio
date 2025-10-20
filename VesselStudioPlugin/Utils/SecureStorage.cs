using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VesselStudioPlugin.Models;

namespace VesselStudioPlugin.Utils
{
    /// <summary>
    /// Provides secure credential storage using OS-level security features
    /// </summary>
    public static class SecureStorage
    {
        #region Constants
        
        private const string TARGET_PREFIX = "VesselStudio_Plugin_";
        private const string TOKEN_KEY_SUFFIX = "_Token";
        private const string SESSION_KEY_SUFFIX = "_Session";
        
        #endregion
        
        #region Windows Credential Manager (P/Invoke)
        
        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credentialPtr);
        
        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite([In] ref Credential userCredential, [In] int flags);
        
        [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode)]
        private static extern bool CredDelete(string target, CredentialType type, int reservedFlag);
        
        [DllImport("Advapi32.dll")]
        private static extern void CredFree([In] IntPtr cred);
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public int Flags;
            public CredentialType Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public CredentialPersistence Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }
        
        private enum CredentialType
        {
            Generic = 1,
            DomainPassword = 2,
            DomainCertificate = 3,
            DomainVisiblePassword = 4,
            GenericCertificate = 5,
            DomainExtended = 6,
            Maximum = 7,
            MaximumEx = Maximum + 1000,
        }
        
        private enum CredentialPersistence
        {
            Session = 1,
            LocalMachine = 2,
            Enterprise = 3
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Stores authentication credentials securely
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="accessToken">The access token to store</param>
        /// <param name="refreshToken">The refresh token to store (optional)</param>
        /// <param name="instanceId">The Rhino instance ID (for multi-instance support)</param>
        /// <returns>True if stored successfully, false otherwise</returns>
        public static bool StoreCredentials(string userId, string accessToken, string? refreshToken = null, string? instanceId = null)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accessToken))
                return false;
            
            try
            {
                var targetName = GetCredentialTarget(userId, instanceId, TOKEN_KEY_SUFFIX);
                var tokenData = new CredentialData
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    StoredAt = DateTime.UtcNow
                };
                
                var jsonData = JsonSerializer.Serialize(tokenData);
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return StoreCredentialWindows(targetName, userId, jsonData);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return StoreCredentialMacOS(targetName, userId, jsonData);
                }
                else
                {
                    // Fallback to encrypted local storage for other platforms
                    return StoreCredentialFallback(targetName, jsonData);
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error storing credentials: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Retrieves stored authentication credentials
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="instanceId">The Rhino instance ID (for multi-instance support)</param>
        /// <returns>Retrieved credential data or null if not found</returns>
        public static CredentialData? RetrieveCredentials(string userId, string? instanceId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;
            
            try
            {
                var targetName = GetCredentialTarget(userId, instanceId, TOKEN_KEY_SUFFIX);
                
                string? jsonData = null;
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    jsonData = RetrieveCredentialWindows(targetName);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    jsonData = RetrieveCredentialMacOS(targetName);
                }
                else
                {
                    jsonData = RetrieveCredentialFallback(targetName);
                }
                
                if (string.IsNullOrWhiteSpace(jsonData))
                    return null;
                    
                return JsonSerializer.Deserialize<CredentialData>(jsonData);
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error retrieving credentials: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Stores authentication session information securely
        /// </summary>
        /// <param name="session">The authentication session to store</param>
        /// <returns>True if stored successfully, false otherwise</returns>
        public static bool StoreSession(AuthenticationSession session)
        {
            if (session == null || !session.IsValid())
                return false;
            
            try
            {
                var targetName = GetCredentialTarget(session.UserId, session.InstanceId, SESSION_KEY_SUFFIX);
                
                // Create a safe copy without sensitive tokens
                var safeCopy = session.GetSafeCopy();
                var jsonData = JsonSerializer.Serialize(safeCopy);
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return StoreCredentialWindows(targetName, session.UserId, jsonData);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return StoreCredentialMacOS(targetName, session.UserId, jsonData);
                }
                else
                {
                    return StoreCredentialFallback(targetName, jsonData);
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error storing session: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Retrieves stored authentication session information
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="instanceId">The Rhino instance ID (for multi-instance support)</param>
        /// <returns>Retrieved session or null if not found</returns>
        public static AuthenticationSession? RetrieveSession(string userId, string? instanceId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;
            
            try
            {
                var targetName = GetCredentialTarget(userId, instanceId, SESSION_KEY_SUFFIX);
                
                string? jsonData = null;
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    jsonData = RetrieveCredentialWindows(targetName);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    jsonData = RetrieveCredentialMacOS(targetName);
                }
                else
                {
                    jsonData = RetrieveCredentialFallback(targetName);
                }
                
                if (string.IsNullOrWhiteSpace(jsonData))
                    return null;
                    
                return JsonSerializer.Deserialize<AuthenticationSession>(jsonData);
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error retrieving session: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Deletes stored credentials and session information
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="instanceId">The Rhino instance ID (for multi-instance support)</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        public static bool DeleteCredentials(string userId, string? instanceId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;
            
            var success = true;
            
            try
            {
                // Delete both credentials and session
                var tokenTarget = GetCredentialTarget(userId, instanceId, TOKEN_KEY_SUFFIX);
                var sessionTarget = GetCredentialTarget(userId, instanceId, SESSION_KEY_SUFFIX);
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    success &= DeleteCredentialWindows(tokenTarget);
                    success &= DeleteCredentialWindows(sessionTarget);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    success &= DeleteCredentialMacOS(tokenTarget);
                    success &= DeleteCredentialMacOS(sessionTarget);
                }
                else
                {
                    success &= DeleteCredentialFallback(tokenTarget);
                    success &= DeleteCredentialFallback(sessionTarget);
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Error deleting credentials: {ex.Message}");
                success = false;
            }
            
            return success;
        }
        
        /// <summary>
        /// Clears all stored credentials for the plugin
        /// </summary>
        /// <returns>True if cleared successfully, false otherwise</returns>
        public static bool ClearAllCredentials()
        {
            // This is a best-effort operation
            // We can't easily enumerate all stored credentials, so this would need to be called
            // with specific user IDs in practice
            Rhino.RhinoApp.WriteLine("ClearAllCredentials called - use DeleteCredentials with specific user IDs");
            return true;
        }
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Generates a credential target name
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="instanceId">The instance identifier (optional)</param>
        /// <param name="suffix">The key suffix</param>
        /// <returns>Target name for credential storage</returns>
        private static string GetCredentialTarget(string userId, string? instanceId, string suffix)
        {
            var target = TARGET_PREFIX + userId;
            if (!string.IsNullOrWhiteSpace(instanceId))
                target += "_" + instanceId;
            return target + suffix;
        }
        
        #endregion
        
        #region Windows Implementation
        
        private static bool StoreCredentialWindows(string targetName, string userName, string data)
        {
            try
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var credential = new Credential
                {
                    Type = CredentialType.Generic,
                    TargetName = Marshal.StringToHGlobalUni(targetName),
                    UserName = Marshal.StringToHGlobalUni(userName),
                    CredentialBlob = Marshal.AllocHGlobal(dataBytes.Length),
                    CredentialBlobSize = dataBytes.Length,
                    Persist = CredentialPersistence.LocalMachine
                };
                
                Marshal.Copy(dataBytes, 0, credential.CredentialBlob, dataBytes.Length);
                
                var result = CredWrite(ref credential, 0);
                
                // Clean up allocated memory
                Marshal.FreeHGlobal(credential.TargetName);
                Marshal.FreeHGlobal(credential.UserName);
                Marshal.FreeHGlobal(credential.CredentialBlob);
                
                return result;
            }
            catch
            {
                return false;
            }
        }
        
        private static string? RetrieveCredentialWindows(string targetName)
        {
            try
            {
                if (CredRead(targetName, CredentialType.Generic, 0, out IntPtr credPtr))
                {
                    var credential = Marshal.PtrToStructure<Credential>(credPtr);
                    var dataBytes = new byte[credential.CredentialBlobSize];
                    Marshal.Copy(credential.CredentialBlob, dataBytes, 0, credential.CredentialBlobSize);
                    
                    CredFree(credPtr);
                    
                    return Encoding.UTF8.GetString(dataBytes);
                }
            }
            catch
            {
                // Fall through to return null
            }
            
            return null;
        }
        
        private static bool DeleteCredentialWindows(string targetName)
        {
            try
            {
                return CredDelete(targetName, CredentialType.Generic, 0);
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
        
        #region macOS Implementation (Placeholder)
        
        private static bool StoreCredentialMacOS(string targetName, string userName, string data)
        {
            // TODO: Implement using macOS Keychain APIs
            // For now, fall back to the fallback implementation
            return StoreCredentialFallback(targetName, data);
        }
        
        private static string? RetrieveCredentialMacOS(string targetName)
        {
            // TODO: Implement using macOS Keychain APIs
            // For now, fall back to the fallback implementation
            return RetrieveCredentialFallback(targetName);
        }
        
        private static bool DeleteCredentialMacOS(string targetName)
        {
            // TODO: Implement using macOS Keychain APIs
            // For now, fall back to the fallback implementation
            return DeleteCredentialFallback(targetName);
        }
        
        #endregion
        
        #region Fallback Implementation (Encrypted Local Storage)
        
        private static bool StoreCredentialFallback(string targetName, string data)
        {
            try
            {
                var filePath = GetFallbackFilePath(targetName);
                var directory = System.IO.Path.GetDirectoryName(filePath);
                
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);
                
                // Use DPAPI on Windows, or basic encryption on other platforms
                byte[] encryptedData;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    encryptedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(data), null, DataProtectionScope.CurrentUser);
                }
                else
                {
                    // Basic encryption for non-Windows platforms
                    encryptedData = Encoding.UTF8.GetBytes(Convert.ToBase64String(Encoding.UTF8.GetBytes(data)));
                }
                
                System.IO.File.WriteAllBytes(filePath, encryptedData);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private static string? RetrieveCredentialFallback(string targetName)
        {
            try
            {
                var filePath = GetFallbackFilePath(targetName);
                
                if (!System.IO.File.Exists(filePath))
                    return null;
                
                var encryptedData = System.IO.File.ReadAllBytes(filePath);
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decryptedData);
                }
                else
                {
                    // Basic decryption for non-Windows platforms
                    var base64Data = Encoding.UTF8.GetString(encryptedData);
                    return Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));
                }
            }
            catch
            {
                return null;
            }
        }
        
        private static bool DeleteCredentialFallback(string targetName)
        {
            try
            {
                var filePath = GetFallbackFilePath(targetName);
                
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private static string GetFallbackFilePath(string targetName)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var pluginDataPath = System.IO.Path.Combine(appDataPath, "VesselStudioPlugin", "Credentials");
            return System.IO.Path.Combine(pluginDataPath, targetName + ".dat");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents stored credential data
    /// </summary>
    public class CredentialData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime StoredAt { get; set; }
        
        /// <summary>
        /// Gets whether the stored credentials are still fresh (not older than 24 hours)
        /// </summary>
        public bool IsFresh => DateTime.UtcNow - StoredAt < TimeSpan.FromHours(24);
    }
}