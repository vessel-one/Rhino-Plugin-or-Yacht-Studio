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
            // Form properties - larger to accommodate content
            this.Text = "Vessel Studio - Image Settings";
            this.Width = 480;           // Increased from 400
            this.Height = 380;          // Increased from 280
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = System.Drawing.Color.White;
            this.Padding = new System.Windows.Forms.Padding(15);  // Form padding

            // Image Format Group Box - larger with better spacing
            _imageGroupBox = new GroupBox();
            _imageGroupBox.Text = "Image Format Settings";
            _imageGroupBox.Left = 15;
            _imageGroupBox.Top = 15;
            _imageGroupBox.Width = 450;         // Increased from 360
            _imageGroupBox.Height = 240;        // Increased from 180
            _imageGroupBox.Padding = new System.Windows.Forms.Padding(15);  // Internal padding

            // Format Label
            _formatLabel = new Label();
            _formatLabel.Text = "Format:";
            _formatLabel.Left = 15;
            _formatLabel.Top = 25;
            _formatLabel.Width = 80;
            _formatLabel.Height = 20;
            _formatLabel.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);

            // Format ComboBox - larger
            _formatCombo = new ComboBox();
            _formatCombo.Left = 110;
            _formatCombo.Top = 25;
            _formatCombo.Width = 310;          // Increased from 240
            _formatCombo.Height = 25;
            _formatCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _formatCombo.Font = new System.Drawing.Font("Arial", 9);
            _formatCombo.Items.AddRange(new[] { "PNG (Recommended - Lossless)", "JPEG (Smaller file size)" });
            _formatCombo.SelectedIndexChanged += FormatCombo_SelectedIndexChanged;

            // Format Description Label - larger with better spacing
            _formatDescLabel = new Label();
            _formatDescLabel.Text = "PNG:  Lossless quality, maintains 100% fidelity\n         Larger file size (~2-3 MB)\n\nJPEG:  Compressed with adjustable quality\n         Smaller file size (~500-800 KB)";
            _formatDescLabel.Left = 15;
            _formatDescLabel.Top = 60;
            _formatDescLabel.Width = 405;      // Increased from 330
            _formatDescLabel.Height = 75;      // Increased from 40
            _formatDescLabel.Font = new System.Drawing.Font("Arial", 8.5f);
            _formatDescLabel.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);

            // Quality Label
            _qualityLabel = new Label();
            _qualityLabel.Text = "JPEG Quality:";
            _qualityLabel.Left = 15;
            _qualityLabel.Top = 150;
            _qualityLabel.Width = 90;
            _qualityLabel.Height = 20;
            _qualityLabel.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);

            // Quality Slider - larger
            _qualitySlider = new TrackBar();
            _qualitySlider.Left = 15;
            _qualitySlider.Top = 175;
            _qualitySlider.Width = 320;        // Increased from 250
            _qualitySlider.Height = 40;
            _qualitySlider.Minimum = 1;
            _qualitySlider.Maximum = 100;
            _qualitySlider.Value = 95;
            _qualitySlider.TickFrequency = 10;
            _qualitySlider.ValueChanged += QualitySlider_ValueChanged;

            // Quality Value Label
            _qualityValueLabel = new Label();
            _qualityValueLabel.Text = "95";
            _qualityValueLabel.Left = 345;
            _qualityValueLabel.Top = 178;
            _qualityValueLabel.Width = 60;
            _qualityValueLabel.Height = 20;
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

            // OK Button - larger with proper spacing
            _okButton = new Button();
            _okButton.Text = "OK";
            _okButton.DialogResult = DialogResult.OK;
            _okButton.Left = 270;
            _okButton.Top = 270;
            _okButton.Width = 90;
            _okButton.Height = 35;
            _okButton.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);
            _okButton.Click += OkButton_Click;

            // Cancel Button - larger with proper spacing
            _cancelButton = new Button();
            _cancelButton.Text = "Cancel";
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.Left = 375;
            _cancelButton.Top = 270;
            _cancelButton.Width = 90;
            _cancelButton.Height = 35;
            _cancelButton.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);

            // Add controls to form
            this.Controls.Add(_imageGroupBox);
            this.Controls.Add(_okButton);
            this.Controls.Add(_cancelButton);

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
