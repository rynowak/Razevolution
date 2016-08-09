using System;
using System.Threading;
using Razevolution.Tooling.Messages;

namespace Razevolution.Tooling
{
    public abstract class MessageProcessor
    {
        private readonly CancellationTokenSource _cancel;
        private readonly MessageQueue _queue;
        private readonly Thread _thread;

        public MessageProcessor(MessageQueue queue)
        {
            _queue = queue;

            _cancel = new CancellationTokenSource();
            _thread = new Thread(ProcessMessages) { IsBackground = true, };
        }

        protected abstract void ProcessMessage(Message message);

        public void Start()
        {
            if (_thread.IsAlive)
            {
                throw new InvalidOperationException();
            }

            _thread.Start();
        }

        public void Wait()
        {
            if (_thread.IsAlive)
            {
                _thread.Join();
            }
        }

        public void Stop()
        {
            if (!_thread.IsAlive)
            {
                throw new InvalidOperationException();
            }

            _cancel.Cancel();
            _thread.Join();
        }

        private void ProcessMessages(object state)
        {
            while (!_cancel.IsCancellationRequested && !_queue.IsCompleted)
            {
                Message message;

                try
                {
                    message = _queue.Take();
                }
                catch (InvalidOperationException)
                {
                    // This will happen if the queue is completed before calling Take(), which can happen due to a race.
                    return;
                }
                catch (OperationCanceledException)
                {
                    // This will happen if the queue is completed while inside Take().
                    return;
                }

                ProcessMessage(message);
            }
        }
    }
}
