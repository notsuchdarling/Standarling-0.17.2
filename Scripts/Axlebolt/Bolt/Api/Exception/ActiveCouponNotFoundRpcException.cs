using Axlebolt.RpcSupport;

namespace Axlebolt.Bolt.Api.Exception
{
    public class ActiveCouponNotFoundRpcException : RpcException
    {
        public ActiveCouponNotFoundRpcException()
            : base(3801)
        {
        }
    }
}
