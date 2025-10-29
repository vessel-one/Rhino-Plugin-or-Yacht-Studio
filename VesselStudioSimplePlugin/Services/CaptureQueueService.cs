using System;
using System.Collections.Generic;
using System.Linq;
using VesselStudioSimplePlugin.Models;

namespace VesselStudioSimplePlugin.Services
{
    /// <summary>
    /// Singleton service managing the queued batch capture workflow.
    /// 
    /// Provides thread-safe operations for:
    /// - T011: Create CaptureQueueService singleton instance
    /// - T012: Thread-safe Add/Remove/Clear/GetItems methods with lock
    /// - T013: Event handlers (ItemAdded, ItemRemoved, QueueCleared) for UI updates
    /// - T014: Enforce 20-item queue limit
    /// - T015: Auto-assign sequence numbers for chronological ordering
    /// </summary>
    public class CaptureQueueService
    {
        /// <summary>
        /// Static instance - singleton pattern for plugin lifecycle.
        /// Lazy<T> ensures thread-safe initialization on first access.
        /// </summary>
        private static readonly Lazy<CaptureQueueService> _instance = 
            new Lazy<CaptureQueueService>(() => new CaptureQueueService());

        /// <summary>
        /// Gets the singleton instance of CaptureQueueService.
        /// </summary>
        public static CaptureQueueService Current
        {
            get { return _instance.Value; }
        }

        /// <summary>
        /// The current queue model containing all queued captures.
        /// </summary>
        private CaptureQueue _queue;

        /// <summary>
        /// Lock object for thread-safe operations on the queue.
        /// All operations (Add, Remove, Clear, GetItems) acquire this lock.
        /// </summary>
        private readonly object _lockObject = new object();

        /// <summary>
        /// T013: Event raised when an item is added to the queue.
        /// Listeners can update UI to show new queue count.
        /// </summary>
        public event EventHandler<ItemAddedEventArgs> ItemAdded;

        /// <summary>
        /// T013: Event raised when an item is removed from the queue.
        /// Listeners can update UI to remove visual representation of item.
        /// </summary>
        public event EventHandler<ItemRemovedEventArgs> ItemRemoved;

        /// <summary>
        /// T013: Event raised when the entire queue is cleared.
        /// Listeners can reset UI (e.g., hide dialog, clear thumbnails, reset count).
        /// </summary>
        public event EventHandler QueueCleared;

        /// <summary>
        /// Private constructor for singleton pattern.
        /// Initializes an empty queue at service creation.
        /// </summary>
        private CaptureQueueService()
        {
            _queue = new CaptureQueue();
        }

        /// <summary>
        /// Gets the current queue instance (read-only reference).
        /// Direct access for queries that don't modify the queue.
        /// </summary>
        public CaptureQueue Queue
        {
            get
            {
                lock (_lockObject)
                {
                    return _queue;
                }
            }
        }

        /// <summary>
        /// T012: Thread-safe method to add an item to the queue.
        /// T014: Enforces 20-item maximum limit before adding.
        /// T015: Auto-assigns sequence number based on current max + 1.
        /// 
        /// Behavior:
        /// 1. Acquire lock for thread safety
        /// 2. Check queue is not at capacity (20 items max)
        /// 3. Auto-assign sequence number (max sequence in queue + 1)
        /// 4. Add to queue
        /// 5. Raise ItemAdded event for UI update
        /// 6. Release lock
        /// </summary>
        /// <param name="item">The QueuedCaptureItem to add</param>
        /// <exception cref="ArgumentNullException">Thrown if item is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if queue is full (20 items max)</exception>
        public void AddItem(QueuedCaptureItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "Item cannot be null");

            lock (_lockObject)
            {
                // T014: Enforce 20-item queue limit
                if (_queue.Count >= 20)
                    throw new InvalidOperationException(
                        $"Queue is full (maximum 20 items). Current count: {_queue.Count}");

                // T015: Auto-assign sequence number based on current max + 1
                // If queue is empty, start at 1; otherwise use max + 1
                int nextSequence = _queue.Items.Count == 0
                    ? 1
                    : _queue.Items.Max(x => x.SequenceNumber) + 1;
                item.SequenceNumber = nextSequence;

                // Add item to queue
                _queue.Items.Add(item);

                // T013: Raise ItemAdded event for UI notification
                OnItemAdded(new ItemAddedEventArgs { Item = item, QueueCount = _queue.Count });
            }
        }

        /// <summary>
        /// T012: Thread-safe method to remove a specific item from the queue.
        /// 
        /// Behavior:
        /// 1. Acquire lock for thread safety
        /// 2. Remove item from queue (disposes it)
        /// 3. Raise ItemRemoved event for UI update
        /// 4. Release lock
        /// </summary>
        /// <param name="item">The QueuedCaptureItem to remove</param>
        /// <returns>True if item was removed, false if not found</returns>
        public bool RemoveItem(QueuedCaptureItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "Item cannot be null");

            lock (_lockObject)
            {
                bool removed = _queue.Remove(item);

                if (removed)
                {
                    // T013: Raise ItemRemoved event for UI notification
                    OnItemRemoved(new ItemRemovedEventArgs { Item = item, QueueCount = _queue.Count });
                }

                return removed;
            }
        }

        /// <summary>
        /// T012: Thread-safe method to remove item at specific index.
        /// 
        /// Behavior:
        /// 1. Acquire lock for thread safety
        /// 2. Remove item at index (disposes it)
        /// 3. Raise ItemRemoved event for UI update
        /// 4. Release lock
        /// </summary>
        /// <param name="index">Zero-based index of item to remove</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index out of range</exception>
        public void RemoveItemAt(int index)
        {
            lock (_lockObject)
            {
                if (index < 0 || index >= _queue.Count)
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        $"Index {index} out of range. Queue contains {_queue.Count} items");

                var removedItem = _queue.Items[index];
                _queue.RemoveAt(index);

                // T013: Raise ItemRemoved event for UI notification
                OnItemRemoved(new ItemRemovedEventArgs { Item = removedItem, QueueCount = _queue.Count });
            }
        }

        /// <summary>
        /// T012: Thread-safe method to clear entire queue.
        /// 
        /// Behavior:
        /// 1. Acquire lock for thread safety
        /// 2. Clear all items (disposes each item)
        /// 3. Raise QueueCleared event for UI notification
        /// 4. Release lock
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _queue.Clear();

                // T013: Raise QueueCleared event for UI notification
                OnQueueCleared();
            }
        }

        /// <summary>
        /// T012: Thread-safe method to get a copy of all current items.
        /// Returns a snapshot copy to prevent external modification of the queue.
        /// 
        /// Behavior:
        /// 1. Acquire lock for thread safety
        /// 2. Create a new List<> with current items
        /// 3. Release lock
        /// 4. Return copy (safe for external iteration)
        /// </summary>
        /// <returns>A new List<QueuedCaptureItem> containing a snapshot of current items</returns>
        public List<QueuedCaptureItem> GetItems()
        {
            lock (_lockObject)
            {
                // Return a copy to prevent external modification
                return new List<QueuedCaptureItem>(_queue.Items);
            }
        }

        /// <summary>
        /// Gets the current number of items in the queue.
        /// </summary>
        public int ItemCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _queue.Count;
                }
            }
        }

        /// <summary>
        /// Gets whether the queue is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock (_lockObject)
                {
                    return _queue.IsEmpty;
                }
            }
        }

        /// <summary>
        /// Gets whether the queue is full (at 20-item capacity).
        /// </summary>
        public bool IsFull
        {
            get
            {
                lock (_lockObject)
                {
                    return _queue.Count >= 20;
                }
            }
        }

        /// <summary>
        /// Gets remaining capacity (items that can be added).
        /// </summary>
        public int RemainingCapacity
        {
            get
            {
                lock (_lockObject)
                {
                    return 20 - _queue.Count;
                }
            }
        }

        /// <summary>
        /// Gets total size of all image data in bytes.
        /// </summary>
        public long TotalSizeBytes
        {
            get
            {
                lock (_lockObject)
                {
                    return _queue.TotalSizeBytes;
                }
            }
        }

        /// <summary>
        /// Gets or sets the project name associated with this queue.
        /// </summary>
        public string ProjectName
        {
            get
            {
                lock (_lockObject)
                {
                    return _queue.ProjectName;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _queue.ProjectName = value;
                }
            }
        }

        /// <summary>
        /// Raises the ItemAdded event.
        /// </summary>
        protected virtual void OnItemAdded(ItemAddedEventArgs e)
        {
            ItemAdded?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the ItemRemoved event.
        /// </summary>
        protected virtual void OnItemRemoved(ItemRemovedEventArgs e)
        {
            ItemRemoved?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the QueueCleared event.
        /// </summary>
        protected virtual void OnQueueCleared()
        {
            QueueCleared?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clears the singleton instance (for testing or cleanup).
        /// After this is called, a new instance will be created on next access.
        /// </summary>
        internal static void ResetInstance()
        {
            _instance.Value?.Clear();
        }
    }

    /// <summary>
    /// T013: Event arguments for ItemAdded event.
    /// </summary>
    public class ItemAddedEventArgs : EventArgs
    {
        /// <summary>
        /// The item that was added to the queue.
        /// </summary>
        public QueuedCaptureItem Item { get; set; }

        /// <summary>
        /// The new queue count after adding the item.
        /// </summary>
        public int QueueCount { get; set; }
    }

    /// <summary>
    /// T013: Event arguments for ItemRemoved event.
    /// </summary>
    public class ItemRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// The item that was removed from the queue.
        /// </summary>
        public QueuedCaptureItem Item { get; set; }

        /// <summary>
        /// The new queue count after removing the item.
        /// </summary>
        public int QueueCount { get; set; }
    }
}
