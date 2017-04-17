using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ThreadingLib.Interfaces
{
   public interface IActionQueue
   {
      Task<T> QueueWorkItem<T>(Func<T> f);

      Task QueueWorkItem(Action a);

      Task QueueWorkItemAt(DateTimeOffset dateTime, Action a);

      Task<T> QueueWorkItemAt<T>(DateTimeOffset dateTime, Func<T> a);
   }
}