using System;
using Epic.OnlineServices;
using Mirage.SocketLayer;

namespace Mirage.Sockets.EpicSocket
{
    internal sealed class EpicEndPoint : IEndPoint
    {
        public ProductUserId UserId;

        public EpicEndPoint() { }
        private EpicEndPoint(ProductUserId userId)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        }

        IEndPoint IEndPoint.CreateCopy()
        {
            return new EpicEndPoint(UserId);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EpicEndPoint other))
                return false;

            // both null
            if (UserId == null && UserId == other.UserId)
                return true;

            return UserId.Equals(other.UserId);
        }

        public override int GetHashCode()
        {
            if (UserId == null)
                return 0;

            return UserId.GetHashCode();
        }
    }
}

