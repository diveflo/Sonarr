// Blacklist has been deprecated for blocklist.
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Datastore;
using Sonarr.Api.V3.Blocklist;
using Sonarr.Http;
using Sonarr.Http.Extensions;

namespace Sonarr.Api.V3.Blacklist
{
    public class BlacklistModule : SonarrRestModule<BlocklistResource>
    {
        private readonly BlocklistService _blocklistService;

        public BlacklistModule(BlocklistService blocklistService)
        {
            _blocklistService = blocklistService;
            GetResourcePaged = Blocklist;
            DeleteResource = DeleteBlockList;

            Delete("/bulk", x => Remove());
        }

        private PagingResource<BlocklistResource> Blocklist(PagingResource<BlocklistResource> pagingResource)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<BlocklistResource, NzbDrone.Core.Blocklisting.Blocklist>("date", SortDirection.Descending);

            return ApplyToPage(_blocklistService.Paged, pagingSpec, BlocklistResourceMapper.MapToResource);
        }

        private void DeleteBlockList(int id)
        {
            _blocklistService.Delete(id);
        }

        private object Remove()
        {
            var resource = Request.Body.FromJson<BlocklistBulkResource>();

            _blocklistService.Delete(resource.Ids);

            return new object();
        }
    }
}
