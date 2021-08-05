using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepReality.Utils{

    /// <summary>
    /// Utilities to better use the async/await C# features.
    /// </summary>
    public static class AsyncUtils
    {
        public static int UnityThreadId
        {
            get; private set;
        }

        public static SynchronizationContext UnitySynchronizationContext
        {
            get; private set;
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            UnitySynchronizationContext = SynchronizationContext.Current;
            UnityThreadId = Thread.CurrentThread.ManagedThreadId;
        }


        public struct SynchronizationContextAwaiter : INotifyCompletion
        {
            private static readonly SendOrPostCallback _postCallback = state => ((Action)state)();

            private readonly SynchronizationContext _context;
            public SynchronizationContextAwaiter(SynchronizationContext context)
            {
                _context = context;
            }

            public bool IsCompleted => _context == SynchronizationContext.Current;

            public void OnCompleted(Action continuation) => _context.Post(_postCallback, continuation);

            public void GetResult() { }
        }


        public static SynchronizationContextAwaiter GetAwaiter(this SynchronizationContext context)
        {
            return new SynchronizationContextAwaiter(context);
        }

        public static async Task WaitForMainThreadAsync()
        {
            await UnitySynchronizationContext;
        }

        public static async Task WaitForUpdate()
        {
            await UpdateCoroutine().ToTask();
        }

        public static Task ToTask(this IEnumerator enumerator)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            if (SynchronizationContext.Current == UnitySynchronizationContext)
            {
                SessionManager.Instance.StartCoroutine(AwaiterCoroutine(enumerator, tcs));
            }
            else
            {
                UnitySynchronizationContext.Post(_ => SessionManager.Instance.StartCoroutine(AwaiterCoroutine(enumerator, tcs)), null);
            }

            return tcs.Task;

            
        }

        static IEnumerator UpdateCoroutine()
        {
            yield return null;
        }

        static IEnumerator AwaiterCoroutine(IEnumerator toYeld, TaskCompletionSource<bool> toSet)
        {
            yield return toYeld;
            toSet.SetResult(true);
        }
    }

    

}
