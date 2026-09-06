using ThreeInARow.Domain.Meta;

namespace ThreeInARow.Application
{
    public interface IProfileStore
    {
        ProfileState LoadOrCreate();
        void Save(ProfileState profile);
    }

    internal sealed class MemoryProfileStore : IProfileStore
    {
        private ProfileState _profile = ProfileProgression.CreateFresh();

        public ProfileState LoadOrCreate()
        {
            return _profile;
        }

        public void Save(ProfileState profile)
        {
            _profile = profile ?? ProfileProgression.CreateFresh();
        }
    }
}
