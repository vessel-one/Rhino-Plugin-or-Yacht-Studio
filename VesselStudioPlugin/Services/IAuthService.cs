using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VesselStudioPlugin.Models;

namespace VesselStudioPlugin.Services
{
    /// <summary>
    /// Interface for authentication service that manages user login and session state
    /// </summary>
    public interface IAuthService : IDisposable
    {
        /// <summary>
        /// Gets the current authentication state
        /// </summary>
        AuthenticationState CurrentState { get; }
        
        /// <summary>
        /// Gets a value indicating whether the user is currently authenticated
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// Gets the current user ID if authenticated
        /// </summary>
        string? CurrentUserId { get; }
        
        /// <summary>
        /// Gets the current access token if authenticated
        /// </summary>
        string? CurrentToken { get; }
        
        /// <summary>
        /// Event raised when authentication state changes
        /// </summary>
        event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;
        
        /// <summary>
        /// Initiates the authentication flow by opening the user's browser
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if authentication succeeded, false otherwise</returns>
        Task<bool> LoginAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Logs out the current user and clears stored credentials
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        Task LogoutAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Attempts to refresh the current authentication token
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if token refresh succeeded, false otherwise</returns>
        Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validates the current authentication token
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if token is valid, false otherwise</returns>
        Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Loads the user's accessible projects from Vessel Studio
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>List of projects the user can access</returns>
        Task<List<ProjectInfo>> GetUserProjectsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if the user has upload permissions for the specified project
        /// </summary>
        /// <param name="projectId">The project ID to check</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if user has upload permissions, false otherwise</returns>
        Task<bool> HasProjectPermissionAsync(string projectId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Initializes the service by attempting to restore a previous session
        /// </summary>
        /// <returns>True if session was restored, false otherwise</returns>
        Task<bool> InitializeAsync();
        
        /// <summary>
        /// Gets the authentication session information
        /// </summary>
        /// <returns>Current session info or null if not authenticated</returns>
        AuthenticationSession? GetCurrentSession();
        
        /// <summary>
        /// Authenticates the user using OAuth device flow
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if authentication succeeded, false otherwise</returns>
        Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Signs out the current user
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        Task SignOutAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the current access token
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The access token if available, null otherwise</returns>
        Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets user information for the current authenticated user
        /// </summary>
        /// <returns>User information if authenticated, null otherwise</returns>
        UserInfo? GetUserInfo();
        
        /// <summary>
        /// Gets the current session information
        /// </summary>
        AuthenticationSession? CurrentSession { get; }
    }
    
    /// <summary>
    /// Event arguments for authentication state changes
    /// </summary>
    public class AuthenticationStateChangedEventArgs : EventArgs
    {
        public AuthenticationState PreviousState { get; }
        public AuthenticationState NewState { get; }
        public string? Message { get; }
        
        public AuthenticationStateChangedEventArgs(
            AuthenticationState previousState, 
            AuthenticationState newState, 
            string? message = null)
        {
            PreviousState = previousState;
            NewState = newState;
            Message = message;
        }
    }
}