using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eto.Drawing;
using Eto.Forms;
using VesselStudioPlugin.Models;
using VesselStudioPlugin.Services;

namespace VesselStudioPlugin.UI
{
    /// <summary>
    /// Dialog for selecting a Vessel Studio project
    /// </summary>
    public class ProjectSelectionDialog : Dialog<ProjectInfo?>
    {
        #region Fields and Controls
        
        private readonly IApiClient _apiClient;
        private readonly IAuthService _authService;
        
        private GridView _projectGrid;
        private Button _refreshButton;
        private Button _selectButton;
        private Button _cancelButton;
        private Label _statusLabel;
        private TextBox _searchBox;
        private ProgressBar _progressBar;
        
        private List<ProjectInfo> _allProjects = new List<ProjectInfo>();
        private List<ProjectInfo> _filteredProjects = new List<ProjectInfo>();
        private ProjectInfo? _selectedProject;
        
        private CancellationTokenSource? _loadCancellationSource;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of ProjectSelectionDialog
        /// </summary>
        /// <param name="apiClient">API client for project data</param>
        /// <param name="authService">Authentication service</param>
        public ProjectSelectionDialog(IApiClient apiClient, IAuthService authService)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            
            InitializeDialog();
            InitializeControls();
            SetupLayout();
            SetupEventHandlers();
            
            // Load projects on dialog show
            Shown += async (_, _) => await LoadProjectsAsync();
        }
        
        #endregion
        
        #region Dialog Setup
        
        /// <summary>
        /// Initializes dialog properties
        /// </summary>
        private void InitializeDialog()
        {
            Title = "Select Vessel Studio Project";
            MinimumSize = new Size(600, 400);
            Size = new Size(800, 500);
            Resizable = true;
            Maximizable = false;
            
            // Center on screen
            WindowStyle = WindowStyle.Default;
            ShowInTaskbar = false;
        }
        
        /// <summary>
        /// Initializes all UI controls
        /// </summary>
        private void InitializeControls()
        {
            // Search box
            _searchBox = new TextBox
            {
                PlaceholderText = "Search projects...",
                Width = 200
            };
            
            // Project grid
            _projectGrid = new GridView
            {
                BackgroundColor = Colors.White,
                GridLines = GridLines.Horizontal,
                AllowMultipleSelection = false,
                ShowHeader = true
            };
            
            SetupProjectGrid();
            
            // Status label
            _statusLabel = new Label
            {
                Text = "Loading projects...",
                TextColor = Colors.Gray
            };
            
            // Progress bar
            _progressBar = new ProgressBar
            {
                Visible = false
            };
            
            // Buttons
            _refreshButton = new Button
            {
                Text = "Refresh",
                Image = Icon.FromResource("refresh.ico", typeof(ProjectSelectionDialog)),
                Width = 100
            };
            
            _selectButton = new Button
            {
                Text = "Select",
                Width = 100,
                Enabled = false
            };
            
            _cancelButton = new Button
            {
                Text = "Cancel",
                Width = 100
            };
            
            // Set default/abort buttons
            DefaultButton = _selectButton;
            AbortButton = _cancelButton;
        }
        
        /// <summary>
        /// Sets up the project grid columns
        /// </summary>
        private void SetupProjectGrid()
        {
            _projectGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<ProjectInfo, string>(p => p.Name) },
                HeaderText = "Project Name",
                Width = 200,
                Resizable = true,
                Sortable = true
            });
            
            _projectGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<ProjectInfo, string>(p => p.Description ?? "") },
                HeaderText = "Description",
                Width = 300,
                Resizable = true
            });
            
            _projectGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<ProjectInfo, string>(p => p.CreatedAt.ToString("yyyy-MM-dd")) },
                HeaderText = "Created",
                Width = 100,
                Resizable = true,
                Sortable = true
            });
            
            _projectGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<ProjectInfo, string>(p => p.UpdatedAt.ToString("yyyy-MM-dd")) },
                HeaderText = "Modified",
                Width = 100,
                Resizable = true,
                Sortable = true
            });
            
            _projectGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<ProjectInfo, string>(p => p.IsPublic ? "Public" : "Private") },
                HeaderText = "Visibility",
                Width = 80,
                Resizable = true
            });
        }
        
        /// <summary>
        /// Sets up the dialog layout
        /// </summary>
        private void SetupLayout()
        {
            var searchLayout = new DynamicLayout();
            searchLayout.BeginHorizontal();
            searchLayout.Add(new Label { Text = "Search:" });
            searchLayout.Add(_searchBox, true);
            searchLayout.Add(_refreshButton);
            searchLayout.EndHorizontal();
            
            var buttonLayout = new DynamicLayout();
            buttonLayout.BeginHorizontal();
            buttonLayout.Add(null, true); // Spacer
            buttonLayout.Add(_selectButton);
            buttonLayout.Add(_cancelButton);
            buttonLayout.EndHorizontal();
            
            var statusLayout = new DynamicLayout();
            statusLayout.BeginHorizontal();
            statusLayout.Add(_statusLabel, true);
            statusLayout.Add(_progressBar);
            statusLayout.EndHorizontal();
            
            var mainLayout = new DynamicLayout { Padding = 10, Spacing = new Size(5, 5) };
            mainLayout.AddRow(searchLayout);
            mainLayout.AddRow(_projectGrid, true);
            mainLayout.AddRow(statusLayout);
            mainLayout.AddRow(buttonLayout);
            
            Content = mainLayout;
        }
        
        /// <summary>
        /// Sets up event handlers for all controls
        /// </summary>
        private void SetupEventHandlers()
        {
            // Search box
            _searchBox.TextChanged += (_, _) => FilterProjects();
            
            // Project grid
            _projectGrid.SelectionChanged += OnProjectSelectionChanged;
            _projectGrid.CellDoubleClick += OnProjectDoubleClick;
            
            // Buttons
            _refreshButton.Click += async (_, _) => await LoadProjectsAsync();
            _selectButton.Click += OnSelectProject;
            _cancelButton.Click += OnCancel;
            
            // Dialog events
            Closing += OnDialogClosing;
        }
        
        #endregion
        
        #region Project Loading
        
        /// <summary>
        /// Loads projects from the API
        /// </summary>
        private async Task LoadProjectsAsync()
        {
            if (!_authService.IsAuthenticated)
            {
                UpdateStatus("Not authenticated. Please sign in first.", true);
                return;
            }
            
            try
            {
                // Cancel any existing load operation
                _loadCancellationSource?.Cancel();
                _loadCancellationSource = new CancellationTokenSource();
                
                // Update UI to show loading state
                SetLoadingState(true);
                UpdateStatus("Loading projects...", false);
                
                // Load projects from API
                var projects = await _apiClient.GetProjectsAsync(_loadCancellationSource.Token);
                
                if (projects != null)
                {
                    _allProjects = projects.ToList();
                    FilterProjects();
                    
                    var projectCount = _allProjects.Count;
                    UpdateStatus($"Loaded {projectCount} project{(projectCount != 1 ? "s" : "")}", false);
                }
                else
                {
                    UpdateStatus("Failed to load projects", true);
                }
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Loading cancelled", false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading projects: {ex.Message}", true);
            }
            finally
            {
                SetLoadingState(false);
            }
        }
        
        /// <summary>
        /// Filters projects based on search text
        /// </summary>
        private void FilterProjects()
        {
            var searchText = _searchBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            
            if (string.IsNullOrEmpty(searchText))
            {
                _filteredProjects = _allProjects.ToList();
            }
            else
            {
                _filteredProjects = _allProjects.Where(p =>
                    p.Name.ToLowerInvariant().Contains(searchText) ||
                    (p.Description?.ToLowerInvariant().Contains(searchText) == true)
                ).ToList();
            }
            
            // Update grid data source
            _projectGrid.DataStore = _filteredProjects;
            
            // Update status
            if (!string.IsNullOrEmpty(searchText))
            {
                var filteredCount = _filteredProjects.Count;
                var totalCount = _allProjects.Count;
                UpdateStatus($"Showing {filteredCount} of {totalCount} projects", false);
            }
        }
        
        #endregion
        
        #region UI State Management
        
        /// <summary>
        /// Sets the loading state of the dialog
        /// </summary>
        /// <param name="isLoading">Whether loading is in progress</param>
        private void SetLoadingState(bool isLoading)
        {
            _refreshButton.Enabled = !isLoading;
            _projectGrid.Enabled = !isLoading;
            _progressBar.Visible = isLoading;
            
            if (isLoading)
            {
                _progressBar.Indeterminate = true;
            }
            else
            {
                _progressBar.Indeterminate = false;
            }
        }
        
        /// <summary>
        /// Updates the status label
        /// </summary>
        /// <param name="message">Status message</param>
        /// <param name="isError">Whether this is an error message</param>
        private void UpdateStatus(string message, bool isError)
        {
            _statusLabel.Text = message;
            _statusLabel.TextColor = isError ? Colors.Red : Colors.Gray;
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handles project selection changes
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        private void OnProjectSelectionChanged(object? sender, EventArgs e)
        {
            _selectedProject = _projectGrid.SelectedItem as ProjectInfo;
            _selectButton.Enabled = _selectedProject != null;
            
            if (_selectedProject != null)
            {
                UpdateStatus($"Selected: {_selectedProject.Name}", false);
            }
        }
        
        /// <summary>
        /// Handles project double-click (same as select)
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        private void OnProjectDoubleClick(object? sender, GridCellMouseEventArgs e)
        {
            if (_selectedProject != null)
            {
                OnSelectProject(sender, EventArgs.Empty);
            }
        }
        
        /// <summary>
        /// Handles select button click
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        private void OnSelectProject(object? sender, EventArgs e)
        {
            if (_selectedProject != null)
            {
                Result = _selectedProject;
                Close();
            }
        }
        
        /// <summary>
        /// Handles cancel button click
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        private void OnCancel(object? sender, EventArgs e)
        {
            Result = null;
            Close();
        }
        
        /// <summary>
        /// Handles dialog closing
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        private void OnDialogClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Cancel any pending load operations
            _loadCancellationSource?.Cancel();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Shows the dialog and returns the selected project
        /// </summary>
        /// <param name="parent">Parent control</param>
        /// <returns>Selected project or null if cancelled</returns>
        public new ProjectInfo? ShowModal(Control? parent = null)
        {
            return base.ShowModal(parent);
        }
        
        /// <summary>
        /// Pre-selects a project by ID
        /// </summary>
        /// <param name="projectId">Project ID to select</param>
        public void SelectProject(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
                return;
            
            var project = _filteredProjects.FirstOrDefault(p => p.Id == projectId);
            if (project != null)
            {
                _projectGrid.SelectedItem = project;
            }
        }
        
        /// <summary>
        /// Sets the search filter
        /// </summary>
        /// <param name="searchText">Search text to filter by</param>
        public void SetSearchFilter(string searchText)
        {
            _searchBox.Text = searchText ?? string.Empty;
            FilterProjects();
        }
        
        #endregion
        
        #region Cleanup
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loadCancellationSource?.Cancel();
                _loadCancellationSource?.Dispose();
            }
            
            base.Dispose(disposing);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Factory for creating project selection dialogs
    /// </summary>
    public static class ProjectSelectionDialogFactory
    {
        /// <summary>
        /// Creates a new project selection dialog
        /// </summary>
        /// <param name="apiClient">API client service</param>
        /// <param name="authService">Authentication service</param>
        /// <returns>New dialog instance</returns>
        public static ProjectSelectionDialog Create(IApiClient apiClient, IAuthService authService)
        {
            return new ProjectSelectionDialog(apiClient, authService);
        }
        
        /// <summary>
        /// Shows a project selection dialog and returns the result
        /// </summary>
        /// <param name="apiClient">API client service</param>
        /// <param name="authService">Authentication service</param>
        /// <param name="parent">Parent control</param>
        /// <param name="preSelectedProjectId">Project ID to pre-select</param>
        /// <returns>Selected project or null if cancelled</returns>
        public static ProjectInfo? ShowDialog(
            IApiClient apiClient, 
            IAuthService authService, 
            Control? parent = null,
            string? preSelectedProjectId = null)
        {
            using var dialog = Create(apiClient, authService);
            
            if (!string.IsNullOrEmpty(preSelectedProjectId))
            {
                dialog.SelectProject(preSelectedProjectId);
            }
            
            return dialog.ShowModal(parent);
        }
    }
}