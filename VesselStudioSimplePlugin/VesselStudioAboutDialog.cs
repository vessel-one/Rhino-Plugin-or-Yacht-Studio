using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Rhino;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// About dialog showing version info and credits
    /// </summary>
    public class VesselStudioAboutDialog : Form
    {
        public VesselStudioAboutDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Get version from assembly
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}";

            Text = "About Vessel Studio Plugin";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            AutoSize = false;
            
            // Layout constants
            const int contentWidth = 460;
            const int padding = 20;
            int yPos = 0;

            // Logo/Header
            var headerPanel = new Panel
            {
                Location = new Point(0, yPos),
                Width = contentWidth + (padding * 2),
                Height = 90,
                BackColor = Color.FromArgb(37, 99, 235) // Blue
            };
            Controls.Add(headerPanel);

            var titleLabel = new Label
            {
                Text = "Vessel Studio",
                Location = new Point(padding, 20),
                Width = contentWidth,
                AutoSize = true,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(titleLabel);

            var subtitleLabel = new Label
            {
                Text = "Rhino Plugin",
                Location = new Point(padding, 50),
                Width = contentWidth,
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                ForeColor = Color.FromArgb(219, 234, 254),
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(subtitleLabel);
            yPos += headerPanel.Height + padding;

            // Version info
            var versionLabel = new Label
            {
                Text = $"Version {versionString}",
                Location = new Point(padding, yPos),
                Width = contentWidth,
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            Controls.Add(versionLabel);
            yPos += versionLabel.Height + 5;

            var buildLabel = new Label
            {
                Text = $"Build for Rhino {RhinoApp.Version.Major}",
                Location = new Point(padding, yPos),
                Width = contentWidth,
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            Controls.Add(buildLabel);
            yPos += buildLabel.Height + 20;

            // Description with wrapping
            var descriptionLabel = new Label
            {
                Text = "Capture and share your yacht designs with the Vessel Studio platform. Upload viewport screenshots, manage projects, and collaborate with your team.",
                Location = new Point(padding, yPos),
                Width = contentWidth,
                MaximumSize = new Size(contentWidth, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            Controls.Add(descriptionLabel);
            yPos += descriptionLabel.Height + 20;

            // Features section
            var featuresLabel = new Label
            {
                Text = "Features:",
                Location = new Point(padding, yPos),
                Width = contentWidth,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            Controls.Add(featuresLabel);
            yPos += featuresLabel.Height + 8;

            var featuresText = new Label
            {
                Text = "• Viewport Screenshot Capture\n" +
                       "• Secure API Key Authentication\n" +
                       "• Project Management Integration\n" +
                       "• Automatic Metadata Collection\n" +
                       "• Cross-platform Support (Windows & Mac)",
                Location = new Point(padding + 20, yPos),
                Width = contentWidth - 20,
                MaximumSize = new Size(contentWidth - 20, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            Controls.Add(featuresText);
            yPos += featuresText.Height + 20;

            // Links row
            int linkX = padding;
            var websiteLink = new LinkLabel
            {
                Text = "vesselstudio.io",
                Location = new Point(linkX, yPos),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                LinkColor = Color.FromArgb(37, 99, 235),
                ActiveLinkColor = Color.FromArgb(29, 78, 216)
            };
            websiteLink.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://vesselstudio.io");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            Controls.Add(websiteLink);
            linkX += 120;

            var docsLink = new LinkLabel
            {
                Text = "Documentation",
                Location = new Point(linkX, yPos),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                LinkColor = Color.FromArgb(37, 99, 235),
                ActiveLinkColor = Color.FromArgb(29, 78, 216)
            };
            docsLink.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://vesselstudio.io/docs/rhino-plugin");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            Controls.Add(docsLink);
            linkX += 120;

            var supportLink = new LinkLabel
            {
                Text = "Support",
                Location = new Point(linkX, yPos),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                LinkColor = Color.FromArgb(37, 99, 235),
                ActiveLinkColor = Color.FromArgb(29, 78, 216)
            };
            supportLink.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://vesselstudio.io/support");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            Controls.Add(supportLink);
            yPos += 30;

            // Close button (right-aligned)
            var closeButton = new Button
            {
                Text = "Close",
                Width = 80,
                Height = 32,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            closeButton.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            closeButton.Location = new Point(padding + contentWidth - closeButton.Width, yPos);
            Controls.Add(closeButton);
            yPos += closeButton.Height + 10;

            // Copyright (centered at bottom)
            var copyrightLabel = new Label
            {
                Text = "© 2025 Creata Collective Limited (NZ). All rights reserved.",
                Location = new Point(padding, yPos),
                Width = contentWidth,
                AutoSize = false,
                Height = 20,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(copyrightLabel);
            yPos += copyrightLabel.Height + padding;

            // Set final form size
            ClientSize = new Size(contentWidth + (padding * 2), yPos);

            AcceptButton = closeButton;
            CancelButton = closeButton;
        }
    }
}
