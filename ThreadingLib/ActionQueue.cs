using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadingLib
{
   internal class WorkItem
   {
      private readonly Action _a;
      private readonly TaskCompletionSource<bool> _completed;

      public Task Task => _completed.Task;

      public WorkItem(Action a)
      {
         _a = a;
         _completed = new TaskCompletionSource<bool>();
      }

      public void ProcessWorkItem()
      {
         _a?.Invoke(); // Null check in case someone queues a null action.
         _completed.SetResult(true);
      }
   }

   public class ActionQueue
   {
      private readonly Queue<WorkItem> _actionQueue =
         new Queue<WorkItem>();

      private readonly ManualResetEvent _mutex = new ManualResetEvent(false);

      private readonly object _lock = new object();

      public ActionQueue()
      {
         var thread = new Thread(ProceessWorkItems) { IsBackground = true };
         thread.Start();
      }

      public Task QueueWorkItem(Action a)
      {
         var workItem = new WorkItem(a);

         lock (_lock)
         {
            //  The enqueue and set must be atomic so done within the lock.
            _actionQueue.Enqueue(workItem);
            _mutex.Set();
         }

         return workItem.Task;
      }

      private void ProceessWorkItems()
      {
         while (true)
         {
            WorkItem result = null;
            bool wasAny;

            lock (_lock)
            {
               wasAny = _actionQueue.Any();

               if (wasAny)
               {
                  result = _actionQueue.Dequeue();
               }
               else
               {
                  _mutex.Reset();
               }
            }

            if (wasAny)
            {
               // Process the work item outside the lock to avoid blocking
               // Enqueuers. This action may be long-running, so we don't
               // want to block inside the lock for a long time.
               result.ProcessWorkItem();
            }
            else
            {
               // If there wasn't any work items when we checked queue length,
               // wait on the mutex. It's possible that we'll immediately continue
               // here if someone enqueued something between the check and this WaitOne.
               // Otherwise, we just wait for some work to do...
               _mutex.WaitOne();
            }
         }
      }
   }
}
