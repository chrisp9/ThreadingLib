using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadingLib.Internal
{
   public interface IWorkItem
   {
      void ProcessWorkItem();
   }


   internal class WorkItem<T> : IWorkItem
   {
      private readonly Func<T> _f;
      private readonly TaskCompletionSource<T> _completed = new TaskCompletionSource<T>();

      public Task<T> Task => _completed.Task;

      public WorkItem(Func<T> f)
      {
         _f = f;
      }

      public void ProcessWorkItem()
      {
         if (_f != null)
         {
            var result = _f.Invoke();
            _completed.SetResult(result);
         }
         else
         {
            _completed.SetResult(default(T));
         }
      }
   }
}
