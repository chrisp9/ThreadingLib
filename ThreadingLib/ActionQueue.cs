﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThreadingLib.Interfaces;
using ThreadingLib.Internal;

namespace ThreadingLib
{
   public class ActionQueue : IActionQueue
   {
      private readonly Queue<IWorkItem> _actionQueue =
         new Queue<IWorkItem>();

      private readonly ManualResetEvent _mutex = new ManualResetEvent(false);

      private readonly object _lock = new object();

      public ActionQueue()
      {
         var thread = new Thread(ProceessWorkItems) { IsBackground = true };
         thread.Start();
      }

      public Task<T> QueueWorkItem<T>(Func<T> f)
      {
         var workItem = new WorkItem<T>(f);

         lock (_lock)
         {
            //  The enqueue and set must be atomic so done within the lock.
            _actionQueue.Enqueue(workItem);
            _mutex.Set();
         }

         return workItem.Task;
      }

      public Task QueueWorkItem(Action a)
      {
         return QueueWorkItem(() =>
         {
            a();

            // Return 0 means nothing - we have to return something in order to use
            // the Func<T> overload above. This saves code repetition. 
            return false;
         });
      }

      public Task QueueWorkItemAt(DateTimeOffset dateTime, Action a)
      {
         return QueueWorkItemAt(dateTime, () =>
         {
            a();
            return true;
         });
      }

      public Task<T> QueueWorkItemAt<T>(DateTimeOffset dateTime, Func<T> f)
      {
         var delayPeriod = dateTime - DateTimeOffset.UtcNow;

         if (delayPeriod <= TimeSpan.Zero)
         {
            // If the scheduled time is before the current time, queue
            // the work item immediately
            return QueueWorkItem(f);
         }

         var tcs = new TaskCompletionSource<T>();

         Task.Delay(delayPeriod).ContinueWith(_ =>
         {
            var result = QueueWorkItem(f);
            result.ContinueWith(v => tcs.SetResult(v.Result));
         });

         return tcs.Task;
      }

      private void ProceessWorkItems()
      {
         while (true)
         {
            IWorkItem result = null;
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
