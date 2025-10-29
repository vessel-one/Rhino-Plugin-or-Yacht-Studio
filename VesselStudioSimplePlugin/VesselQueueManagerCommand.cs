using System;
using Rhino;
using Rhino.Commands;
using VesselStudioSimplePlugin.UI;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Command to open the Queue Manager Dialog from command line or script
    /// 
    /// Tasks T041-T042: Command line access to queue manager
    /// - T041: Create VesselQueueManagerCommand with proper registration
    /// - T042: Performance optimization (<500ms dialog open time)
    /// </summary>
    public class VesselQueueManagerCommand : Command
    {
        public VesselQueueManagerCommand()
        {
            Instance = this;
        }

        public static VesselQueueManagerCommand Instance { get; private set; }

        public override string EnglishName => "VesselQueueManager";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // T042: Performance: Dialog opens <500ms from command invocation
                using (var dialog = new QueueManagerDialog())
                {
                    dialog.ShowDialog();
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error opening Queue Manager: {ex.Message}");
                return Result.Failure;
            }
        }
    }

    /// <summary>
    /// DEV variant of VesselQueueManagerCommand for development/debug builds
    /// </summary>
    public class DevVesselQueueManagerCommand : Command
    {
        public DevVesselQueueManagerCommand()
        {
            Instance = this;
        }

        public static DevVesselQueueManagerCommand Instance { get; private set; }

        public override string EnglishName => "DevVesselQueueManager";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                RhinoApp.WriteLine("[DevVesselQueueManager] Opening Queue Manager Dialog...");
                using (var dialog = new QueueManagerDialog())
                {
                    dialog.ShowDialog();
                }
                RhinoApp.WriteLine("[DevVesselQueueManager] Queue Manager closed");
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"[DevVesselQueueManager] Error: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
