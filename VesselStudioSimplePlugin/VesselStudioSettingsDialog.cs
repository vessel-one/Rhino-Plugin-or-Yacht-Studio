using System;
using System.Drawing;
using System.Windows.Forms;
using Rhino;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Settings dialog for Vessel Studio API configuration
    /// </summary>
    public class VesselStudioSettingsDialog : Form
    {
        private TextBox _apiKeyTextBox;
        private Button _validateButton;
        private Button _saveButton;
        private Button _cancelButton;
        private TextBox _outputTextBox;
        private LinkLabel _getApiKeyLink;
        private VesselStudioApiClient _apiClient;
        private bool _isValidated = false;

        public string ApiKey => _apiKeyTextBox.Text.Trim();

        public VesselStudioSettingsDialog(string currentApiKey = "")
        {
            InitializeComponents();
            _apiKeyTextBox.Text = currentApiKey;
            _apiClient = new VesselStudioApiClient();
        }

        private void InitializeComponents()
        {
            // Form settings
            this.Text = "Vessel Studio Settings";
            this.Size = new Size(560, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 240);

            // Title
            var titleLabel = new Label
            {
                Text = "Rhino Plugin API Configuration",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(500, 30),
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            this.Controls.Add(titleLabel);

            // API Key label
            var apiKeyLabel = new Label
            {
                Text = "API Key:",
                Font = new Font("Segoe UI", 10),
                Location = new Point(20, 65),
                Size = new Size(100, 20)
            };
            this.Controls.Add(apiKeyLabel);

            // API Key textbox
            _apiKeyTextBox = new TextBox
            {
                Location = new Point(20, 90),
                Size = new Size(500, 30),
                Font = new Font("Consolas", 10)
            };
            _apiKeyTextBox.TextChanged += (s, e) => _isValidated = false;
            this.Controls.Add(_apiKeyTextBox);

            // Get API key link
            _getApiKeyLink = new LinkLabel
            {
                Text = "Get your API key from Vessel Studio",
                Location = new Point(20, 125),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9),
                LinkColor = Color.FromArgb(70, 130, 180)
            };
            _getApiKeyLink.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://vesselstudio.io/settings?tab=rhino");
                }
                catch { }
            };
            this.Controls.Add(_getApiKeyLink);

            // Validate button
            _validateButton = new Button
            {
                Text = "Validate API Key",
                Location = new Point(20, 155),
                Size = new Size(500, 40),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _validateButton.FlatAppearance.BorderSize = 0;
            _validateButton.Click += OnValidateClick;
            this.Controls.Add(_validateButton);

            // Output console label
            var outputLabel = new Label
            {
                Text = "Output:",
                Font = new Font("Segoe UI", 10),
                Location = new Point(20, 210),
                Size = new Size(100, 20)
            };
            this.Controls.Add(outputLabel);

            // Output textbox (console-style)
            _outputTextBox = new TextBox
            {
                Location = new Point(20, 235),
                Size = new Size(500, 150),
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.FromArgb(0, 255, 0),
                ScrollBars = ScrollBars.Vertical,
                Text = "Ready to validate API key..."
            };
            this.Controls.Add(_outputTextBox);

            // Save button
            _saveButton = new Button
            {
                Text = "Save",
                Location = new Point(330, 405),
                Size = new Size(90, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            _saveButton.FlatAppearance.BorderSize = 0;
            _saveButton.Click += OnSaveClick;
            this.Controls.Add(_saveButton);

            // Cancel button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(430, 405),
                Size = new Size(90, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.FromArgb(50, 50, 50),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            _cancelButton.FlatAppearance.BorderSize = 0;
            this.Controls.Add(_cancelButton);
        }

        private async void OnValidateClick(object sender, EventArgs e)
        {
            var apiKey = _apiKeyTextBox?.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                WriteOutput("❌ ERROR: API key is empty");
                WriteOutput("Please enter your API key");
                return;
            }

            // Validate format
            if (!apiKey.StartsWith("vsk_"))
            {
                WriteOutput("❌ ERROR: Invalid API key format");
                WriteOutput("Key should start with 'vsk_live_' or 'vsk_test_'");
                WriteOutput("");
                WriteOutput("Get your API key from:");
                WriteOutput("https://vesselstudio.io/settings?tab=rhino");
                return;
            }

            // Disable button during validation
            _validateButton.Enabled = false;
            _validateButton.Text = "Validating...";
            WriteOutput("=================================");
            WriteOutput($"Connecting to Vessel Studio API...");
            WriteOutput($"Endpoint: https://vesselstudio.io/api/rhino/validate");
            WriteOutput($"API Key: {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
            WriteOutput("");

            try
            {
                _apiClient.SetApiKey(apiKey);
                WriteOutput("POST /rhino/validate");
                WriteOutput("");
                
                var result = await _apiClient.ValidateApiKeyAsync();

                if (result.Success && result.HasValidSubscription)
                {
                    WriteOutput("=================================");
                    WriteOutput("✅ SUCCESS: API key validated");
                    WriteOutput($"Connected as: {result.UserName}");
                    WriteOutput($"Subscription: Active ({result.UserEmail})");
                    WriteOutput("");
                    WriteOutput("You can now save and start capturing!");
                    WriteOutput("=================================");
                    _isValidated = true;
                    _saveButton.Enabled = true;
                    
                    // Save subscription status immediately
                    var settings = VesselStudioSettings.Load();
                    settings.HasValidSubscription = true;
                    settings.LastSubscriptionCheck = DateTime.Now;
                    settings.SubscriptionErrorMessage = null;
                    settings.UpgradeUrl = null;
                    settings.Save();
                }
                else if (result.Success && !result.HasValidSubscription)
                {
                    // API key is valid but subscription is insufficient
                    WriteOutput("=================================");
                    WriteOutput("🔒 SUBSCRIPTION REQUIRED");
                    WriteOutput("");
                    WriteOutput(result.SubscriptionError?.UserMessage ?? "Upgrade required to use Rhino plugin");
                    WriteOutput("");
                    WriteOutput($"Upgrade at: {result.SubscriptionError?.UpgradeUrl ?? "https://vesselstudio.io/settings?tab=billing"}");
                    WriteOutput("=================================");
                    _isValidated = true; // Key is valid, allow saving
                    _saveButton.Enabled = true;
                    
                    // Save locked subscription status
                    var settings = VesselStudioSettings.Load();
                    settings.HasValidSubscription = false;
                    settings.LastSubscriptionCheck = DateTime.Now;
                    settings.SubscriptionErrorMessage = result.SubscriptionError?.UserMessage;
                    settings.UpgradeUrl = result.SubscriptionError?.UpgradeUrl;
                    settings.Save();
                }
                else
                {
                    WriteOutput("=================================");
                    WriteOutput("❌ FAILED: Could not validate API key");
                    WriteOutput("");
                    WriteOutput($"Error: {result.ErrorMessage}");
                    
                    if (!string.IsNullOrEmpty(result.ErrorDetails))
                    {
                        WriteOutput("");
                        WriteOutput("Details:");
                        WriteOutput(result.ErrorDetails);
                    }
                    
                    WriteOutput("");
                    WriteOutput("Troubleshooting:");
                    WriteOutput("  • Check that you copied the entire key");
                    WriteOutput("  • Verify the key hasn't been revoked");
                    WriteOutput("  • Check your internet connection");
                    WriteOutput("  • Verify the backend server is running");
                    WriteOutput("  • Create a new key at:");
                    WriteOutput("    https://vesselstudio.io/settings?tab=rhino");
                    WriteOutput("=================================");
                    _isValidated = false;
                    _saveButton.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                WriteOutput("=================================");
                WriteOutput($"❌ CRITICAL ERROR: {ex.GetType().Name}");
                WriteOutput("");
                WriteOutput($"Message: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    WriteOutput("");
                    WriteOutput($"Inner exception: {ex.InnerException.Message}");
                }
                
                WriteOutput("");
                WriteOutput("Stack trace:");
                WriteOutput(ex.StackTrace);
                WriteOutput("=================================");
                _isValidated = false;
                _saveButton.Enabled = false;
            }
            finally
            {
                _validateButton.Enabled = true;
                _validateButton.Text = "Validate API Key";
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (!_isValidated)
            {
                WriteOutput("❌ ERROR: Please validate the API key first");
                WriteOutput("Click 'Validate API Key' button");
                DialogResult = DialogResult.None;
                return;
            }

            // Save to settings
            var settings = VesselStudioSettings.Load();
            settings.ApiKey = ApiKey;
            settings.Save();

            WriteOutput("");
            WriteOutput("✅ Settings saved successfully!");
            WriteOutput("");
            WriteOutput("Next steps:");
            WriteOutput("  • Run 'VesselCapture' to upload screenshots");
            WriteOutput("  • Run 'VesselQuickCapture' for quick captures");
        }

        private void WriteOutput(string message)
        {
            if (_outputTextBox?.Text == "Ready to validate API key...")
            {
                _outputTextBox.Text = "";
            }

            _outputTextBox?.AppendText(message + Environment.NewLine);
            if (_outputTextBox != null)
            {
                _outputTextBox.SelectionStart = _outputTextBox.Text.Length;
                _outputTextBox.ScrollToCaret();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _apiClient?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
