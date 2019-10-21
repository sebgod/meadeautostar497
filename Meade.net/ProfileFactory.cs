using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities;

namespace ASCOM.Meade.net
{
    public interface IProfileFactory
    {
        IProfileWrapper Create();
    }

    public class ProfileFactory : IProfileFactory
    {
        public IProfileWrapper Create()
        {
            return new ProfileWrapper();
        }
    }
}
