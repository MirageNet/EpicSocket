using Cysharp.Threading.Tasks;

namespace Mirage.Sockets.EpicSocket
{
    /// <summary>
    /// Call that can wait for Callbacks async
    /// </summary>
    /// <remarks>
    /// This must be a class not a struct, other wise copies will be made and callback wont set the _result field correctly for Wait
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class AsyncWaiter<T>
    {
        private T _result;

        public void Callback(ref T result)
        {
            _result = result;
        }
        public void Callback(T result)
        {
            _result = result;
        }
        public async UniTask<T> Wait()
        {
            while (_result == null)
            {
                await UniTask.Yield();
            }

            return _result;
        }
    }
}

