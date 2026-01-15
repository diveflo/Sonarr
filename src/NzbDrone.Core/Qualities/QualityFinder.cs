using System.Linq;
using NLog;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Qualities
{
    public static class QualityFinder
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(QualityFinder));

        public static Quality FindBySourceAndResolution(QualitySource source, int resolution)
        {
            var matchingQuality = Quality.All.SingleOrDefault(q => q.Source == source && q.Resolution == resolution && !q.Name.EndsWith("HEVC"));

            if (matchingQuality != null)
            {
                return matchingQuality;
            }

            // Handle 576p releases that have a Television or Web source, so they don't get rolled up to Bluray 576p
            if (resolution < 720)
            {
                switch (source)
                {
                    case QualitySource.Television:
                        return Quality.SDTV;
                    case QualitySource.Web:
                        return Quality.WEBDL480p;
                    case QualitySource.WebRip:
                        return Quality.WEBRip480p;
                }
            }

            var matchingResolution = Quality.All.Where(q => q.Resolution == resolution)
                                            .OrderBy(q => q.Source)
                                            .ToList();

            var nearestQuality = Quality.Unknown;

            foreach (var quality in matchingResolution)
            {
                if (quality.Source >= source)
                {
                    nearestQuality = quality;
                    break;
                }
            }

            Logger.Warn("Unable to find exact quality for {0} and {1}. Using {2} as fallback", source, resolution, nearestQuality);

            return nearestQuality;
        }

        public static Quality FindBySourceResolutionAndCodec(QualitySource source, int resolution, LocalEpisode episode)
        {
            var sourceAndResolutionMatch = Quality.All.Where(q => q.Source == source && q.Resolution == resolution).ToList();
            if (sourceAndResolutionMatch.Count() == 1)
            {
                return sourceAndResolutionMatch.SingleOrDefault();
            }

            var hevcQuality = sourceAndResolutionMatch.Single(m => m.Name.EndsWith("HEVC"));
            sourceAndResolutionMatch.Remove(hevcQuality);

            var mediaInfo = episode?.MediaInfo;
            string videoFormat = mediaInfo?.VideoFormat;

            if (videoFormat == "HEVC" || videoFormat == "hevc")
            {
                return hevcQuality;
            }
            else
            {
                return sourceAndResolutionMatch.Single();
            }
        }
    }
}
