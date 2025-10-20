using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Rhino.Geometry;

namespace VesselStudioPlugin.Models
{
    /// <summary>
    /// Captures Rhino viewport state and camera information for screenshot metadata
    /// </summary>
    public class ViewportMetadata
    {
        #region Properties
        
        /// <summary>
        /// Gets or sets the Rhino viewport identifier
        /// </summary>
        [Required]
        public string ViewportName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the display mode name (e.g., "Wireframe", "Shaded", "Rendered")
        /// </summary>
        [Required]
        public string DisplayModeName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the display mode enumeration value
        /// </summary>
        public DisplayMode DisplayMode { get; set; } = DisplayMode.Wireframe;
        
        /// <summary>
        /// Gets or sets the 3D camera location in world coordinates
        /// </summary>
        public Point3d CameraPosition { get; set; }
        
        /// <summary>
        /// Gets or sets the 3D camera look-at point in world coordinates
        /// </summary>
        public Point3d CameraTarget { get; set; }
        
        /// <summary>
        /// Gets or sets the camera up vector
        /// </summary>
        public Vector3d CameraUp { get; set; }
        
        /// <summary>
        /// Gets or sets the viewport pixel dimensions
        /// </summary>
        public System.Drawing.Size ViewportSize { get; set; }
        
        /// <summary>
        /// Gets or sets the Rhino version and build information
        /// </summary>
        public string RhinoVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets additional display settings as key-value pairs
        /// </summary>
        public Dictionary<string, object> DisplaySettings { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets the camera lens length (for perspective views)
        /// </summary>
        public double? LensLength { get; set; }
        
        /// <summary>
        /// Gets or sets whether this is a perspective or parallel projection
        /// </summary>
        public bool IsPerspectiveProjection { get; set; }
        
        /// <summary>
        /// Gets or sets the field of view angle in radians (for perspective views)
        /// </summary>
        public double? FieldOfView { get; set; }
        
        /// <summary>
        /// Gets or sets the viewport's construction plane
        /// </summary>
        public Plane? ConstructionPlane { get; set; }
        
        /// <summary>
        /// Gets or sets when this metadata was captured
        /// </summary>
        public DateTime CaptureTimestamp { get; set; } = DateTime.UtcNow;
        
        // Additional properties for API compatibility
        /// <summary>
        /// Gets or sets the view identifier
        /// </summary>
        public string? ViewId { get; set; }
        
        /// <summary>
        /// Alias for ViewportName - view title
        /// </summary>
        public string ViewTitle 
        { 
            get => ViewportName; 
            set => ViewportName = value; 
        }
        
        /// <summary>
        /// Gets or sets the projection mode description
        /// </summary>
        public string ProjectionMode { get; set; } = "Perspective";
        
        /// <summary>
        /// Alias for CameraPosition - camera location
        /// </summary>
        public Point3d CameraLocation 
        { 
            get => CameraPosition; 
            set => CameraPosition = value; 
        }
        
        /// <summary>
        /// Gets or sets the camera direction vector
        /// </summary>
        public Vector3d CameraDirection { get; set; }
        
        /// <summary>
        /// Alias for CameraTarget - target point
        /// </summary>
        public Point3d TargetPoint 
        { 
            get => CameraTarget; 
            set => CameraTarget = value; 
        }
        
        /// <summary>
        /// Gets or sets the near frustum clipping plane distance
        /// </summary>
        public double FrustumNear { get; set; }
        
        /// <summary>
        /// Gets or sets the far frustum clipping plane distance
        /// </summary>
        public double FrustumFar { get; set; }
        
        /// <summary>
        /// Alias for CaptureTimestamp - when captured
        /// </summary>
        public DateTime CapturedAt 
        { 
            get => CaptureTimestamp; 
            set => CaptureTimestamp = value; 
        }
        
        /// <summary>
        /// Gets or sets the construction plane origin
        /// </summary>
        public Point3d ConstructionPlaneOrigin { get; set; }
        
        /// <summary>
        /// Gets or sets the construction plane normal vector
        /// </summary>
        public Vector3d ConstructionPlaneNormal { get; set; }
        
        /// <summary>
        /// Gets or sets the document file path
        /// </summary>
        public string? DocumentPath { get; set; }
        
        /// <summary>
        /// Gets or sets the document name
        /// </summary>
        public string? DocumentName { get; set; }
        
        /// <summary>
        /// Gets or sets the number of objects in the scene
        /// </summary>
        public int ObjectCount { get; set; }
        
        /// <summary>
        /// Gets or sets the model units
        /// </summary>
        public string ModelUnits { get; set; } = "Millimeters";
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates the viewport metadata for required fields and logical consistency
        /// </summary>
        /// <returns>True if metadata is valid, false otherwise</returns>
        public bool IsValid()
        {
            // Check required fields
            if (string.IsNullOrWhiteSpace(ViewportName))
                return false;
                
            if (string.IsNullOrWhiteSpace(DisplayModeName))
                return false;
            
            // Check viewport size
            if (ViewportSize.Width <= 0 || ViewportSize.Height <= 0)
                return false;
            
            // Check camera positions are not identical
            if (CameraPosition.DistanceTo(CameraTarget) < 0.001)
                return false;
            
            // Check timestamp is not in the future
            if (CaptureTimestamp > DateTime.UtcNow.AddMinutes(1))
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Gets validation errors for this metadata
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(ViewportName))
                errors.Add("ViewportName cannot be empty");
                
            if (string.IsNullOrWhiteSpace(DisplayModeName))
                errors.Add("DisplayModeName cannot be empty");
            
            if (ViewportSize.Width <= 0 || ViewportSize.Height <= 0)
                errors.Add("ViewportSize dimensions must be greater than 0");
            
            if (CameraPosition.DistanceTo(CameraTarget) < 0.001)
                errors.Add("CameraPosition and CameraTarget cannot be identical");
            
            if (CaptureTimestamp > DateTime.UtcNow.AddMinutes(1))
                errors.Add("CaptureTimestamp cannot be in the future");
                
            return errors;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Calculates the distance from camera to target
        /// </summary>
        /// <returns>Distance in world units</returns>
        public double GetCameraDistance()
        {
            return CameraPosition.DistanceTo(CameraTarget);
        }
        
        /// <summary>
        /// Gets the camera direction vector (normalized)
        /// </summary>
        /// <returns>Unit vector pointing from camera to target</returns>
        public Vector3d GetCameraDirection()
        {
            var direction = CameraTarget - CameraPosition;
            direction.Unitize();
            return direction;
        }
        
        /// <summary>
        /// Creates a summary string of the viewport state
        /// </summary>
        /// <returns>Human-readable viewport summary</returns>
        public string GetSummary()
        {
            return $"Viewport: {ViewportName}, Mode: {DisplayModeName}, " +
                   $"Size: {ViewportSize.Width}x{ViewportSize.Height}, " +
                   $"Camera Distance: {GetCameraDistance():F2}";
        }
        
        /// <summary>
        /// Converts to dictionary for serialization
        /// </summary>
        /// <returns>Dictionary representation of metadata</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                [nameof(ViewportName)] = ViewportName,
                [nameof(DisplayModeName)] = DisplayModeName,
                [nameof(DisplayMode)] = DisplayMode.ToString(),
                [nameof(CameraPosition)] = $"{CameraPosition.X},{CameraPosition.Y},{CameraPosition.Z}",
                [nameof(CameraTarget)] = $"{CameraTarget.X},{CameraTarget.Y},{CameraTarget.Z}",
                [nameof(CameraUp)] = $"{CameraUp.X},{CameraUp.Y},{CameraUp.Z}",
                [nameof(ViewportSize)] = $"{ViewportSize.Width}x{ViewportSize.Height}",
                [nameof(RhinoVersion)] = RhinoVersion,
                [nameof(IsPerspectiveProjection)] = IsPerspectiveProjection,
                [nameof(CaptureTimestamp)] = CaptureTimestamp.ToString("O")
            };
            
            if (LensLength.HasValue)
                dict[nameof(LensLength)] = LensLength.Value;
                
            if (FieldOfView.HasValue)
                dict[nameof(FieldOfView)] = FieldOfView.Value;
            
            // Add display settings
            foreach (var setting in DisplaySettings)
            {
                dict[$"DisplaySetting_{setting.Key}"] = setting.Value;
            }
            
            return dict;
        }
        
        #endregion
    }
}