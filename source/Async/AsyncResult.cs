using System;
using System.Threading.Tasks;

namespace Knowit.EpiModules.ImageScaling
{
    internal class AsyncResult : IAsyncResult
    {
        private readonly object _state;
        private readonly Task _task; 
        private readonly bool _completedSynchronously;

        public AsyncResult(AsyncCallback callback, object state, Task task)
        {
            _state = state;
            _task = task;
            _completedSynchronously = _task.IsCompleted;
            _task.ContinueWith(t => callback(this), TaskContinuationOptions.ExecuteSynchronously);
        }

        public Task Task
        {
            get { return _task; } 
        }

        public object AsyncState
        {
            get { return _state; }
        }

        public System.Threading.WaitHandle AsyncWaitHandle
        {
            get { return ((IAsyncResult)_task).AsyncWaitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return _completedSynchronously; }
        }

        public bool IsCompleted
        {
            get { return _task.IsCompleted; }
        }
    }
}
