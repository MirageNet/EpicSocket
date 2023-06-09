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
        private bool _received;

        public void Callback(ref T result)
        {
            _result = result;
            _received = true;
        }
        public void Callback(T result)
        {
            _result = result;
            _received = true;
        }
        public async UniTask<T> Wait()
        {
            while (!_received)
            {
                await UniTask.Yield();
            }

            return _result;
        }
    }
}

