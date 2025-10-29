using System;
using System.Drawing;
using System.Windows.Forms;
using Rhino;

namespace VesselStudioSimplePlugin.UI
{
    /// <summary>
    /// Comprehensive settings dialog for Vessel Studio plugin
    /// Combines API key configuration and image format settings
    /// Uses proper WinForms layout with TabControl and resizable design
    /// </summary>
    public class VesselStudioSettingsDialog : Form
    {
        private TabControl _tabControl;
        
        // API Key tab controls
        private TextBox _apiKeyTextBox;
        private Button _validateButton;
        private Label _statusLabel;
        
        // Image Format tab controls
        private ComboBox _formatCombo;
        private Label _formatDescLabel;
        private TrackBar _qualitySlider;
        private Label _qualityValueLabel;
        private Label _qualityLabel;
        
        // Bottom button panel
        private Button _okButton;
        private Button _cancelButton;

        public VesselStudioSettingsDialog()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            // Form properties - resizable dialog
            this.Text = "Vessel Studio - Settings";
            this.Size = new Size(600, 450);
            this.MinimumSize = new Size(500, 400);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.ShowIcon = false;

            // Main TableLayoutPanel for proper resizing
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            this.Controls.Add(mainLayout);

            // TabControl for organizing settings
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            mainLayout.Controls.Add(_tabControl, 0, 0);

            // API Key Tab
            var apiKeyTab = new TabPage("API Key");
            _tabControl.TabPages.Add(apiKeyTab);
            CreateApiKeyTab(apiKeyTab);

            // Image Format Tab
            var imageFormatTab = new TabPage("Image Format");
            _tabControl.TabPages.Add(imageFormatTab);
            CreateImageFormatTab(imageFormatTab);

            // Bottom button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };
            mainLayout.Controls.Add(buttonPanel, 0, 1);

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 30),
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(3)
            };
            buttonPanel.Controls.Add(_cancelButton);

            _okButton = new Button
            {
                Text = "OK",
                Size = new Size(90, 30),
                DialogResult = DialogResult.OK,
                Margin = new Padding(3)
            };
            _okButton.Click += OnOkClick;
            buttonPanel.Controls.Add(_okButton);

            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private void CreateApiKeyTab(TabPage tabPage)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(15)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tabPage.Controls.Add(layout);

            // Instructions label
            var instructionsLabel = new Label
            {
                Text = "Enter your Vessel Studio API key to enable capture upload functionality.\n\n" +
                       "You can find your API key in your Vessel Studio account settings.",
                AutoSize = true,
                MaximumSize = new Size(550, 0),
                Margin = new Padding(0, 0, 0, 15),
                Padding = new Padding(5)
            };
            layout.Controls.Add(instructionsLabel, 0, 0);

            // API Key label
            var apiKeyLabel = new Label
            {
                Text = "API Key:",
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5)
            };
            layout.Controls.Add(apiKeyLabel, 0, 1);

            // API Key textbox
            _apiKeyTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            layout.Controls.Add(_apiKeyTextBox, 0, 2);

            // Validate button
            _validateButton = new Button
            {
                Text = "Validate API Key",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            _validateButton.Click += OnValidateClick;
            layout.Controls.Add(_validateButton, 0, 3);

            // Status label
            _statusLabel = new Label
            {
                Text = "",
                AutoSize = true,
                MaximumSize = new Size(550, 0),
                ForeColor = SystemColors.ControlText,
                Margin = new Padding(0, 5, 0, 0),
                Padding = new Padding(5)
            };
            layout.Controls.Add(_statusLabel, 0, 4);
        }

        private void CreateImageFormatTab(TabPage tabPage)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(15)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tabPage.Controls.Add(layout);

            // Format label
            var formatLabel = new Label
            {
                Text = "Format:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 7, 10, 5)
            };
            layout.Controls.Add(formatLabel, 0, 0);

            // Format combobox
            _formatCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 0, 10)
            };
            _formatCombo.Items.AddRange(new[] { "PNG (Recommended - Lossless)", "JPEG (Smaller file size)" });
            _formatCombo.SelectedIndexChanged += FormatCombo_SelectedIndexChanged;
            layout.Controls.Add(_formatCombo, 1, 0);

            // Format description
            _formatDescLabel = new Label
            {
                Text = "PNG: Lossless quality, maintains 100% fidelity\n      Larger file size (~2-3 MB)\n\n" +
                       "JPEG: Compressed with adjustable quality\n      Smaller file size (~500-800 KB)",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20),
                ForeColor = SystemColors.GrayText,
                Padding = new Padding(5)
            };
            layout.SetColumnSpan(_formatDescLabel, 2);
            layout.Controls.Add(_formatDescLabel, 0, 1);

            // Quality label
            _qualityLabel = new Label
            {
                Text = "JPEG Quality:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 7, 10, 5)
            };
            layout.Controls.Add(_qualityLabel, 0, 2);

            // Quality value label
            _qualityValueLabel = new Label
            {
                Text = "95",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                Margin = new Padding(0, 5, 0, 5)
            };
            layout.Controls.Add(_qualityValueLabel, 1, 2);

            // Quality slider
            _qualitySlider = new TrackBar
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 100,
                Value = 95,
                TickFrequency = 10,
                Margin = new Padding(0, 0, 0, 10)
            };
            _qualitySlider.ValueChanged += QualitySlider_ValueChanged;
            layout.SetColumnSpan(_qualitySlider, 2);
            layout.Controls.Add(_qualitySlider, 0, 3);
        }

        private void LoadSettings()
        {
            var settings = VesselStudioSettings.Load();
            
            // Load API key
            _apiKeyTextBox.Text = settings.ApiKey ?? "";
            
            // Load image format
            if (settings.ImageFormat?.ToLower() == "jpeg")
            {
                _formatCombo.SelectedIndex = 1;
            }
            else
            {
                _formatCombo.SelectedIndex = 0;
            }
            
            // Load quality
            _qualitySlider.Value = Math.Max(1, Math.Min(100, settings.JpegQuality));
            _qualityValueLabel.Text = _qualitySlider.Value.ToString();
            
            UpdateQualityControlsVisibility();
        }

        private void FormatCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateQualityControlsVisibility();
        }

        private void UpdateQualityControlsVisibility()
        {
            bool isJpeg = _formatCombo.SelectedIndex == 1;
            _qualityLabel.Enabled = isJpeg;
            _qualitySlider.Enabled = isJpeg;
            _qualityValueLabel.Enabled = isJpeg;
        }

        private void QualitySlider_ValueChanged(object sender, EventArgs e)
        {
            _qualityValueLabel.Text = _qualitySlider.Value.ToString();
        }

        private async void OnValidateClick(object sender, EventArgs e)
        {
            string apiKey = _apiKeyTextBox.Text?.Trim();
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _statusLabel.Text = "⚠️ Please enter an API key";
                _statusLabel.ForeColor = Color.DarkOrange;
                return;
            }

            _validateButton.Enabled = false;
            _statusLabel.Text = "Validating...";
            _statusLabel.ForeColor = SystemColors.ControlText;

            try
            {
                var apiClient = new VesselStudioApiClient();
                apiClient.SetApiKey(apiKey);
                var result = await apiClient.ValidateApiKeyAsync();

                if (result.Success)
                {
                    _statusLabel.Text = $"✓ API key is valid - Welcome {result.UserName}!";
                    _statusLabel.ForeColor = Color.Green;
                }
                else
                {
                    _statusLabel.Text = $"✗ {result.ErrorMessage ?? "Invalid API key"}";
                    _statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"✗ Error: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                _validateButton.Enabled = true;
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            var settings = VesselStudioSettings.Load();
            
            // Save API key
            settings.ApiKey = _apiKeyTextBox.Text?.Trim();
            
            // Save image format
            settings.ImageFormat = _formatCombo.SelectedIndex == 1 ? "jpeg" : "png";
            
            // Save quality
            settings.JpegQuality = _qualitySlider.Value;
            
            settings.Save();

            RhinoApp.WriteLine($"✓ Settings saved: API Key={(!string.IsNullOrEmpty(settings.ApiKey) ? "Set" : "Not Set")}, Format={settings.ImageFormat}, Quality={settings.JpegQuality}");
        }
    }
}
