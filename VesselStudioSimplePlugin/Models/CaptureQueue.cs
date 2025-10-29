using System;
using System.Collections.Generic;
using System.Linq;

namespace VesselStudioSimplePlugin.Models
{
    /// <summary>
    /// Collection managing all queued captures for the current session.
    /// 
    /// Task T009: Create CaptureQueue model with core collection functionality
    /// Task T010: Implement computed properties for queue state and validation
    /// </summary>
    public class CaptureQueue
    {
        /// <summary>
        /// Ordered list of captures in chronological order (FIFO - earliest first).
        /// </summary>
        public List<QueuedCaptureItem> Items { get; }

        /// <summary>
        /// When queue was initialized (session start).
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// Associated project from dropdown (applies to all items in queue).
        /// Can be null if no project is selected yet.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Maximum number of items allowed in queue (soft limit for performance).
        /// </summary>
        private const int MaxQueueSize = 20;

        /// <summary>
        /// Initializes a new CaptureQueue instance.
        /// Sets CreatedAt to current time and initializes empty items list.
        /// </summary>
        public CaptureQueue()
        {
            Items = new List<QueuedCaptureItem>();
            CreatedAt = DateTime.Now;
            ProjectName = null;
        }

        /// <summary>
        /// T010: Gets the number of items currently in the queue.
        /// Computed property: Count → Items.Count
        /// </summary>
        public int Count
        {
            get { return Items.Count; }
        }

        /// <summary>
        /// T010: Gets the total size of all image data in bytes.
        /// Computed property: Sum of ImageData.Length for all items.
        /// Used for memory management and UI feedback.
        /// </summary>
        public long TotalSizeBytes
        {
            get
            {
                return Items.Sum(item => item.ImageData.Length);
            }
        }

        /// <summary>
        /// T010: Gets whether the queue is currently empty.
        /// Computed property: IsEmpty → Count == 0
        /// </summary>
        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        /// <summary>
        /// Validates that the queue meets all constraints.
        /// 
        /// T010: Validation rules:
        /// - Items must maintain chronological order (earliest first)
        /// - Maximum 20 items enforced (soft limit for performance)
        /// - All items must have unique Ids
        /// 
        /// Throws InvalidOperationException if validation fails.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if validation fails</exception>
        public void Validate()
        {
            // Rule 1: Check maximum queue size
            if (Items.Count > MaxQueueSize)
                throw new InvalidOperationException(
                    $"Queue exceeds maximum size of {MaxQueueSize} items (current: {Items.Count})");

            // Rule 2: Check chronological order (each item's timestamp should be >= previous)
            for (int i = 1; i < Items.Count; i++)
            {
                if (Items[i].Timestamp < Items[i - 1].Timestamp)
                    throw new InvalidOperationException(
                        $"Queue items are not in chronological order. Item {i} has earlier timestamp than item {i - 1}");
            }

            // Rule 3: Check all items have unique Ids
            var uniqueIds = new HashSet<Guid>();
            foreach (var item in Items)
            {
                if (!uniqueIds.Add(item.Id))
                    throw new InvalidOperationException(
                        $"Duplicate item ID found in queue: {item.Id}");
            }
        }

        /// <summary>
        /// Gets whether the queue can accept more items.
        /// Returns false if at maximum capacity (20 items).
        /// </summary>
        public bool CanAddItems
        {
            get { return Items.Count < MaxQueueSize; }
        }

        /// <summary>
        /// Gets the number of items that can still be added to the queue.
        /// </summary>
        public int RemainingCapacity
        {
            get { return MaxQueueSize - Items.Count; }
        }

        /// <summary>
        /// Clears all items from the queue.
        /// Properly disposes each item to clean up resources.
        /// </summary>
        public void Clear()
        {
            foreach (var item in Items)
            {
                item?.Dispose();
            }
            Items.Clear();
        }

        /// <summary>
        /// Removes a specific item from the queue and disposes it.
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if item was removed, false if not found</returns>
        public bool Remove(QueuedCaptureItem item)
        {
            if (Items.Remove(item))
            {
                item?.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes item at the specified index and disposes it.
        /// </summary>
        /// <param name="index">Zero-based index of item to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index is out of range</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Items.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var item = Items[index];
            Items.RemoveAt(index);
            item?.Dispose();
        }
    }
}
