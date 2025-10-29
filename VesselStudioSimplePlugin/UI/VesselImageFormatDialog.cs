using System;
using System.Windows.Forms;

namespace VesselStudioSimplePlugin.UI
{
    /// <summary>
    /// Settings dialog for Vessel Studio plugin image format and quality preferences
    /// 
    /// Enhancement: Phase 5 Group 3+
    /// - Allows user to choose between JPEG and PNG formats
    /// - For JPEG: Quality slider (1-100)
    /// - For PNG: Lossless compression (no quality setting)
    /// </summary>
    public class VesselImageFormatDialog : Form
    {
        private Label _formatLabel;
        private ComboBox _formatCombo;
        private Label _qualityLabel;
        private TrackBar _qualitySlider;
        private Label _qualityValueLabel;
        private Button _okButton;
        private Button _cancelButton;
        private GroupBox _imageGroupBox;
        private Label _formatDescLabel;

        public VesselImageFormatDialog()
        {
            InitializeDialog();
            LoadSettings();
        }

        private void InitializeDialog()
        {
            // Form properties - start with base size, will auto-adjust
            this.Text = "Vessel Studio - Image Settings";
            this.Width = 530;
            this.Height = 450;          // Start larger for dynamic content
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = System.Drawing.Color.White;
            this.Padding = new System.Windows.Forms.Padding(15);  // Form padding

            // Image Format Group Box - with AutoSize for dynamic expansion
            _imageGroupBox = new GroupBox();
            _imageGroupBox.Text = "Image Format Settings";
            _imageGroupBox.Left = 15;
            _imageGroupBox.Top = 15;
            _imageGroupBox.Width = 485;
            _imageGroupBox.Padding = new System.Windows.Forms.Padding(15);  // Internal padding
            _imageGroupBox.AutoSize = false;  // We'll set explicit height after measuring content

            // Format Label
            _formatLabel = new Label();
            _formatLabel.Text = "Format:";
            _formatLabel.Left = 15;
            _formatLabel.Top = 25;
            _formatLabel.Width = 80;
            _formatLabel.Height = 20;
            _formatLabel.AutoSize = false;
            _formatLabel.Margin = new System.Windows.Forms.Padding(5);
            _formatLabel.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);

            // Format ComboBox
            _formatCombo = new ComboBox();
            _formatCombo.Left = 110;
            _formatCombo.Top = 25;
            _formatCombo.Width = 350;
            _formatCombo.Height = 25;
            _formatCombo.Margin = new System.Windows.Forms.Padding(5);
            _formatCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _formatCombo.Font = new System.Drawing.Font("Arial", 9);
            _formatCombo.Items.AddRange(new[] { "PNG (Recommended - Lossless)", "JPEG (Smaller file size)" });
            _formatCombo.SelectedIndexChanged += FormatCombo_SelectedIndexChanged;

            // Format Description Label - with AutoSize for dynamic height
            _formatDescLabel = new Label();
            _formatDescLabel.Text = "PNG:  Lossless quality, maintains 100% fidelity\n         Larger file size (~2-3 MB)\n\nJPEG:  Compressed with adjustable quality\n         Smaller file size (~500-800 KB)";
            _formatDescLabel.Left = 15;
            _formatDescLabel.Top = 60;
            _formatDescLabel.Width = 440;
            _formatDescLabel.AutoSize = true;  // Auto-expand to fit text
            _formatDescLabel.Margin = new System.Windows.Forms.Padding(5);
            _formatDescLabel.Font = new System.Drawing.Font("Arial", 8.5f);
            _formatDescLabel.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);

            // Calculate position for Quality Label based on description label height
            int qualityLabelTop = _formatDescLabel.Top + _formatDescLabel.Height + 10;

            // Quality Label
            _qualityLabel = new Label();
            _qualityLabel.Text = "JPEG Quality:";
            _qualityLabel.Left = 15;
            _qualityLabel.Top = qualityLabelTop;
            _qualityLabel.Width = 110;
            _qualityLabel.Height = 20;
            _qualityLabel.AutoSize = false;
            _qualityLabel.Margin = new System.Windows.Forms.Padding(5);
            _qualityLabel.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);

            // Quality Slider
            _qualitySlider = new TrackBar();
            _qualitySlider.Left = 15;
            _qualitySlider.Top = qualityLabelTop + 25;
            _qualitySlider.Width = 380;
            _qualitySlider.Height = 40;
            _qualitySlider.Margin = new System.Windows.Forms.Padding(5);
            _qualitySlider.Minimum = 1;
            _qualitySlider.Maximum = 100;
            _qualitySlider.Value = 95;
            _qualitySlider.TickFrequency = 10;
            _qualitySlider.ValueChanged += QualitySlider_ValueChanged;

            // Quality Value Label
            _qualityValueLabel = new Label();
            _qualityValueLabel.Text = "95";
            _qualityValueLabel.Left = 405;
            _qualityValueLabel.Top = qualityLabelTop + 28;
            _qualityValueLabel.Width = 40;
            _qualityValueLabel.Height = 20;
            _qualityValueLabel.AutoSize = false;
            _qualityValueLabel.Margin = new System.Windows.Forms.Padding(5);
            _qualityValueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _qualityValueLabel.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            _qualityValueLabel.ForeColor = System.Drawing.Color.FromArgb(0, 102, 204);

            // Add controls to group box
            _imageGroupBox.Controls.Add(_formatLabel);
            _imageGroupBox.Controls.Add(_formatCombo);
            _imageGroupBox.Controls.Add(_formatDescLabel);
            _imageGroupBox.Controls.Add(_qualityLabel);
            _imageGroupBox.Controls.Add(_qualitySlider);
            _imageGroupBox.Controls.Add(_qualityValueLabel);

            // Set GroupBox height based on all contained controls
            int groupBoxHeight = qualityLabelTop + 60 + 15;  // Quality area height + padding
            _imageGroupBox.Height = groupBoxHeight;

            // Calculate button positions based on GroupBox
            int buttonTopPos = _imageGroupBox.Top + _imageGroupBox.Height + 15;

            // OK Button
            _okButton = new Button();
            _okButton.Text = "OK";
            _okButton.DialogResult = DialogResult.OK;
            _okButton.Left = 315;
            _okButton.Top = buttonTopPos;
            _okButton.Width = 90;
            _okButton.Height = 35;
            _okButton.Margin = new System.Windows.Forms.Padding(5);
            _okButton.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);
            _okButton.Click += OkButton_Click;

            // Cancel Button
            _cancelButton = new Button();
            _cancelButton.Text = "Cancel";
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.Left = 415;
            _cancelButton.Top = buttonTopPos;
            _cancelButton.Width = 90;
            _cancelButton.Height = 35;
            _cancelButton.Margin = new System.Windows.Forms.Padding(5);
            _cancelButton.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);

            // Add controls to form
            this.Controls.Add(_imageGroupBox);
            this.Controls.Add(_okButton);
            this.Controls.Add(_cancelButton);

            // Adjust form height to fit all content
            this.Height = buttonTopPos + 50 + 15;

            // Set accept and cancel buttons
            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
        }

        private void LoadSettings()
        {
            var settings = VesselStudioSettings.Load();
            
            // Set format
            if (settings.ImageFormat?.ToLower() == "jpeg")
            {
                _formatCombo.SelectedIndex = 1;
            }
            else
            {
                _formatCombo.SelectedIndex = 0; // PNG is default
            }

            // Set JPEG quality
            _qualitySlider.Value = Math.Max(1, Math.Min(100, settings.JpegQuality));
            _qualityValueLabel.Text = _qualitySlider.Value.ToString();

            // Update UI based on format
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

        private void OkButton_Click(object sender, EventArgs e)
        {
            var settings = VesselStudioSettings.Load();
            
            // Save format
            settings.ImageFormat = _formatCombo.SelectedIndex == 1 ? "jpeg" : "png";
            
            // Save quality
            settings.JpegQuality = _qualitySlider.Value;
            
            settings.Save();

            Rhino.RhinoApp.WriteLine($"✅ Settings saved: Format={settings.ImageFormat}, JPEG Quality={settings.JpegQuality}");
            this.Close();
        }
    }
}
