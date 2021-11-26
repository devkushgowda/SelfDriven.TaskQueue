using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SelfDriven.Queueable.TaskExecutor
{
    public sealed class ThreadSafeBool
    {
        private object _locker = new object();
        private bool _value;
        public ThreadSafeBool(bool initialValue)
        {
            _value = initialValue;
        }
        public bool Value
        {
            get
            {
                lock (_locker)
                {
                    return _value;
                }
            }
            set
            {
                lock (_locker)
                {
                    _value = value;
                }
            }
        }
    }
    public interface ITaskRequest
    {

    }
    public interface IQueueableTaskExecutor<TaskRequestType> : ITaskRequest where TaskRequestType : class, ITaskRequest
    {
        void Initialize();
        void Execute(TaskRequestType request);
    }
    public class TaskQueue<QueueableTaskExecutorType, TaskRequestType>
        where QueueableTaskExecutorType : class, IQueueableTaskExecutor<TaskRequestType>, new()
        where TaskRequestType : class, ITaskRequest
    {
        private QueueableTaskExecutorType _execotor;
        private object _locker = new object();
        private ThreadSafeBool _isProcessingQueue;
        private ThreadSafeBool _isStopped;
        private ConcurrentQueue<TaskRequestType> concurrentQueue = new ConcurrentQueue<TaskRequestType>();

        /// <summary>
        /// Private constructor
        /// </summary>
        private TaskQueue()
        {

        }

        private static Lazy<TaskQueue<QueueableTaskExecutorType, TaskRequestType>> _lazy = new Lazy<TaskQueue<QueueableTaskExecutorType, TaskRequestType>>(() =>
        {
            var te = new TaskQueue<QueueableTaskExecutorType, TaskRequestType>();
            te.Initialize();
            return te;
        });

        private void Initialize()
        {
            _execotor = new QueueableTaskExecutorType();
            _execotor.Initialize();
            _isProcessingQueue = new ThreadSafeBool(false);
            _isStopped = new ThreadSafeBool(false);
        }

        private void ProcessQueueItems()
        {
            TaskRequestType request;
            while (!_isStopped.Value && concurrentQueue.TryDequeue(out request))
            {
                _execotor.Execute(request);
            }
            _isProcessingQueue.Value = false;
        }
        /// <summary>
        /// If no worker is processing the queue then initiate a worker to finish the pending items.
        /// </summary>
        private void ExecutePendingQueueItems()
        {
            if (!_isProcessingQueue.Value)
            {
                _isProcessingQueue.Value = true;
                Task.Run(() => ProcessQueueItems());
            }
        }
        public static TaskQueue<QueueableTaskExecutorType, TaskRequestType> CreateInstance()
        {
            return _lazy.Value;
        }

        public bool Enqueue(TaskRequestType request)
        {
            lock (_locker)
            {
                var result = false;
                //When stopped, do not accept new items.
                if (!_isStopped.Value)
                {
                    result = true;
                    concurrentQueue.Enqueue(request);
                    ExecutePendingQueueItems();
                }
                return result;
            }
        }

        /// <summary>
        /// Stop processing the pending queue items.
        /// </summary>
        public void Stop()
        {
            lock (_locker)
            {
                _isStopped.Value = true;
            }
        }

        /// <summary>
        /// Start processing pending queue items.
        /// </summary>
        public void Start()
        {
            lock (_locker)
            {
                _isStopped.Value = false;
                if (!concurrentQueue.IsEmpty)
                    ExecutePendingQueueItems();
            }
        }
    }
}

