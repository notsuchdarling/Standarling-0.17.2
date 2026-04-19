using Axlebolt.RpcSupport;

namespace Axlebolt.Bolt.Api.Exception
{
    public class CouponHasAlreadyActivatedRpcException : RpcException
    {
        public CouponHasAlreadyActivatedRpcException() 
            : base(3802)
        {
        }
    }
}
