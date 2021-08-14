// Blacklist has been deprecated for blocklist.
using NzbDrone.Api.Blocklist;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Datastore;
using Sonarr.Http;

namespace NzbDrone.Api.Blacklist
{
    public class BlacklistModule : SonarrRestModule<BlocklistResource>
    {
        private readonly BlocklistService _blocklistService;

        public BlacklistModule(BlocklistService blocklistService)
        {
            _blocklistService = blocklistService;
            GetResourcePaged = Blocklist;
            DeleteResource = DeleteBlockList;
        }

        private PagingResource<BlocklistResource> Blocklist(PagingResource<BlocklistResource> pagingResource)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<BlocklistResource, Core.Blocklisting.Blocklist>("id", SortDirection.Ascending);

            return ApplyToPage(_blocklistService.Paged, pagingSpec, BlocklistResourceMapper.MapToResource);
        }

        private void DeleteBlockList(int id)
        {
            _blocklistService.Delete(id);
        }
    }
}
