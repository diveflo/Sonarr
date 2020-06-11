using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser
{
    public class QualityParser
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(QualityParser));

        private static readonly Regex SourceRegex = new Regex(@"\b(?:
                                                                (?<bluray>BluRay|Blu-Ray|HD-?DVD|BD(?!$))|
                                                                (?<webdl>WEB[-_. ]DL|WEBDL|AmazonHD|iTunesHD|MaxdomeHD|NetflixU?HD|WebHD|[. ]WEB[. ](?:[xh]26[45]|DDP?5[. ]1)|[. ](?-i:WEB)$|\d+0p(?:[-. ]AMZN)?[-. ]WEB[-. ]|WEB-DLMux|\b\s\/\sWEB\s\/\s\b|AMZN[. ]WEB[. ])|
                                                                (?<webrip>WebRip|Web-Rip|WEBMux)|
                                                                (?<hdtv>HDTV)|
                                                                (?<bdrip>BDRip)|
                                                                (?<brrip>BRRip)|
                                                                (?<dvd>DVD|DVDRip|NTSC|PAL|xvidvd)|
                                                                (?<dsr>WS[-_. ]DSR|DSR)|
                                                                (?<pdtv>PDTV)|
                                                                (?<sdtv>SDTV)|
                                                                (?<tvrip>TVRip)
                                                                )(?:\b|$|[ .])",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex RawHDRegex = new Regex(@"\b(?<rawhd>RawHD|1080i[-_. ]HDTV|Raw[-_. ]HD|MPEG[-_. ]?2)\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ProperRegex = new Regex(@"\b(?<proper>proper)\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RepackRegex = new Regex(@"\b(?<repack>repack|rerip)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex VersionRegex = new Regex(@"\dv(?<version>\d)\b|\[v(?<version>\d)\]",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RealRegex = new Regex(@"\b(?<real>REAL)\b",
                                                                RegexOptions.Compiled);

        private static readonly Regex ResolutionRegex = new Regex(@"\b(?:(?<R360p>360p)|(?<R480p>480p|640x480|848x480)|(?<R576p>576p)|(?<R720p>720p|1280x720)|(?<R1080p>1080p|1920x1080|1440p|FHD|1080i)|(?<R2160p>2160p|4k[-_. ](?:UHD|HEVC|BD)|(?:UHD|HEVC|BD)[-_. ]4k))\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CodecRegex = new Regex(@"\b(?:(?<x265>x265)|(?<x264>x264)|(?<h264>h264)|(?<xvidhd>XvidHD)|(?<xvid>Xvid)|(?<divx>divx))\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex OtherSourceRegex = new Regex(@"(?<hdtv>HD[-_. ]TV)|(?<sdtv>SD[-_. ]TV)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AnimeBlurayRegex = new Regex(@"bd(?:720|1080|2160)|(?<=[-_. (\[])bd(?=[-_. )\]])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HighDefPdtvRegex = new Regex(@"hr[-_. ]ws", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RemuxRegex = new Regex(@"\b(?<remux>(BD)?[-_. ]?Remux)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static QualityModel ParseQuality(string name)
        {
            Logger.Debug("Trying to parse quality for {0}", name);

            name = name.Trim();

            var result = ParseQualityName(name);

            // Based on extension
            if (result.Quality == Quality.Unknown && !name.ContainsInvalidPathChars())
            {
                try
                {
                    result.Quality = MediaFileExtensions.GetQualityForExtension(Path.GetExtension(name));
                    result.SourceDetectionSource = QualityDetectionSource.Extension;
                    result.ResolutionDetectionSource = QualityDetectionSource.Extension;
                }
                catch (ArgumentException)
                {
                    // Swallow exception for cases where string contains illegal
                    // path characters.
                }
            }

            return result;
        }

        public static QualityModel ParseQualityName(string name)
        {
            var normalizedName = name.Replace('_', ' ').Trim();
            var result = ParseQualityModifiers(name, normalizedName);

            if (RawHDRegex.IsMatch(normalizedName))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.ResolutionDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.RAWHD;

                return result;
            }

            var sourceMatches = SourceRegex.Matches(normalizedName);
            var sourceMatch = sourceMatches.OfType<Match>().LastOrDefault();
            var resolution = ParseResolution(normalizedName);
            var codecRegex = CodecRegex.Match(normalizedName);
            var remuxMatch = RemuxRegex.IsMatch(normalizedName);

            if (resolution != Resolution.Unknown)
            {
                result.ResolutionDetectionSource = QualityDetectionSource.Name;
            }

            if (sourceMatch != null && sourceMatch.Success)
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;

                if (sourceMatch.Groups["bluray"].Success)
                {
                    if (codecRegex.Groups["xvid"].Success || codecRegex.Groups["divx"].Success)
                    {
                        result.Quality = Quality.DVD;
                        return result;
                    }                    

                    if (codecRegex.Groups["x265"].Success)
                    {
                        switch (resolution)
                        {
                            case Resolution.R720p:
                                result.Quality = Quality.Bluray720pHEVC;
                                break;
                            case Resolution.R1080p:
                                result.Quality = Quality.Bluray1080pHEVC;
                                break;
                            case Resolution.R2160p:
                                result.Quality = Quality.Bluray2160pHEVC;
                                break;
                            default:
                                result.Quality = Quality.Unknown;
                                break;
                        }
                        return result;
                    }
                    if (resolution == Resolution.R2160p)
                    {
                        result.Quality = remuxMatch ? Quality.Bluray2160pRemux : Quality.Bluray2160p;

                        return result;
                    }

                    if (resolution == Resolution.R1080p)
                    {
                        result.Quality = remuxMatch ? Quality.Bluray1080pRemux : Quality.Bluray1080p;
                        return result;
                    }

                    if (resolution == Resolution.R360P || resolution == Resolution.R480P || resolution == Resolution.R576p)
                    {
                        result.Quality = Quality.Bluray480p;
                        return result;
                    }

                    // Treat a remux without a source as 1080p, not 720p.
                    if (remuxMatch)
                    {
                        result.Quality = Quality.Bluray1080pRemux;
                        return result;
                    }

                    result.Quality = Quality.Bluray720p;
                    return result;
                }

                if (sourceMatch.Groups["webdl"].Success)
                {
                    if (codecRegex.Groups["x265"].Success)
                    {
                        switch (resolution)
                        {
                            case Resolution.R720p:
                                result.Quality = Quality.WEBDL720pHEVC;
                                return result;
                            case Resolution.R1080p:
                                result.Quality = Quality.WEBDL1080pHEVC;
                                return result;
                            case Resolution.R2160p:
                                result.Quality = Quality.WEBDL2160pHEVC;
                                return result;
                        }
                    }
                    if (resolution == Resolution.R2160p)
                    {
                        result.Quality = Quality.WEBDL2160p;
                        return result;
                    }

                    if (resolution == Resolution.R1080p)
                    {
                        result.Quality = Quality.WEBDL1080p;
                        return result;
                    }

                    if (resolution == Resolution.R720p)
                    {
                        result.Quality = Quality.WEBDL720p;
                        return result;
                    }

                    if (name.Contains("[WEBDL]"))
                    {
                        result.Quality = Quality.WEBDL720p;
                        return result;
                    }

                    result.Quality = Quality.WEBDL480p;
                    return result;
                }

                if (sourceMatch.Groups["webrip"].Success)
                {
                    if (codecRegex.Groups["x265"].Success)
                    {
                        switch (resolution)
                        {
                            case Resolution.R720p:
                                result.Quality = Quality.WEBRip720pHEVC;
                                return result;
                            case Resolution.R1080p:
                                result.Quality = Quality.WEBRip1080pHEVC;
                                return result;
                            case Resolution.R2160p:
                                result.Quality = Quality.WEBRip2160pHEVC;
                                return result;
                        }
                    }

                    if (resolution == Resolution.R2160p)
                    {
                        result.Quality = Quality.WEBRip2160p;
                        return result;
                    }

                    if (resolution == Resolution.R1080p)
                    {
                        result.Quality = Quality.WEBRip1080p;
                        return result;
                    }

                    if (resolution == Resolution.R720p)
                    {
                        result.Quality = Quality.WEBRip720p;
                        return result;
                    }

                    result.Quality = Quality.WEBRip480p;
                    return result;
                }

                if (sourceMatch.Groups["hdtv"].Success)
                {
                    if (codecRegex.Groups["x265"].Success)
                    {
                        switch (resolution)
                        {
                            case Resolution.R720p:
                                result.Quality = Quality.HDTV720pHEVC;
                                return result;
                            case Resolution.R1080p:
                                result.Quality = Quality.HDTV1080pHEVC;
                                return result;
                            case Resolution.R2160p:
                                result.Quality = Quality.HDTV2160pHEVC;
                                return result;
                        }
                    }
                    if (resolution == Resolution.R2160p)
                    {
                        result.Quality = Quality.HDTV2160p;
                        return result;
                    }

                    if (resolution == Resolution.R1080p)
                    {
                        result.Quality = Quality.HDTV1080p;
                        return result;
                    }

                    if (resolution == Resolution.R720p)
                    {
                        result.Quality = Quality.HDTV720p;
                        return result;
                    }

                    if (name.Contains("[HDTV]"))
                    {
                        result.Quality = Quality.HDTV720p;
                        return result;
                    }

                    result.Quality = Quality.SDTV;
                    return result;
                }

                if (sourceMatch.Groups["bdrip"].Success ||
                    sourceMatch.Groups["brrip"].Success)
                {
                    switch (resolution)
                    {
                        case Resolution.R720p:
                            result.Quality = Quality.Bluray720p;
                            return result;
                        case Resolution.R1080p:
                            result.Quality = Quality.Bluray1080p;
                            return result;
                        default:
                            result.Quality = Quality.Bluray480p;
                            return result;
                    }
                }

                if (sourceMatch.Groups["dvd"].Success)
                {
                    if (codecRegex.Groups["x265"].Success)
                    {
                        result.Quality = Quality.DVDHEVC;
                        return result;
                    }
                    result.Quality = Quality.DVD;
                    return result;
                }

                if (sourceMatch.Groups["pdtv"].Success ||
                    sourceMatch.Groups["sdtv"].Success ||
                    sourceMatch.Groups["dsr"].Success ||
                    sourceMatch.Groups["tvrip"].Success)
                {
                    if (resolution == Resolution.R1080p || normalizedName.ContainsIgnoreCase("1080p"))
                    {
                        result.Quality = Quality.HDTV1080p;
                        return result;
                    }

                    if (resolution == Resolution.R720p || normalizedName.ContainsIgnoreCase("720p"))
                    {
                        result.Quality = Quality.HDTV720p;
                        return result;
                    }

                    if (HighDefPdtvRegex.IsMatch(normalizedName))
                    {
                        result.ResolutionDetectionSource = QualityDetectionSource.Name;
                        result.Quality = Quality.HDTV720p;
                        return result;
                    }

                    result.Quality = Quality.SDTV;
                    return result;
                }
            }

            // Anime Bluray matching
            if (AnimeBlurayRegex.Match(normalizedName).Success)
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;

                if (resolution == Resolution.R360P || resolution == Resolution.R480P ||
                    resolution == Resolution.R576p || normalizedName.ContainsIgnoreCase("480p"))
                {
                    result.ResolutionDetectionSource = QualityDetectionSource.Name;
                    result.Quality = Quality.DVD;

                    return result;
                }

                if (resolution == Resolution.R1080p || normalizedName.ContainsIgnoreCase("1080p"))
                {
                    result.ResolutionDetectionSource = QualityDetectionSource.Name;
                    result.Quality = remuxMatch ? Quality.Bluray1080pRemux : Quality.Bluray1080p;

                    return result;
                }

                if (resolution == Resolution.R2160p || normalizedName.ContainsIgnoreCase("2160p"))
                {
                    result.ResolutionDetectionSource = QualityDetectionSource.Name;
                    result.Quality = remuxMatch ? Quality.Bluray2160pRemux : Quality.Bluray2160p;

                    return result;
                }

                // Treat a remux without a source as 1080p, not 720p.
                if (remuxMatch)
                {
                    result.Quality = Quality.Bluray1080p;
                    return result;
                }

                result.Quality = Quality.Bluray720p;
                return result;
            }

            if (resolution != Resolution.Unknown)
            {
                var source = QualitySource.Unknown;

                if (remuxMatch)
                {
                    result.SourceDetectionSource = QualityDetectionSource.Name;
                    source = QualitySource.BlurayRaw;
                }
                else
                {
                    var quality = MediaFileExtensions.GetQualityForExtension(Path.GetExtension(name));

                    if (quality != Quality.Unknown)
                    {
                        result.SourceDetectionSource = QualityDetectionSource.Extension;
                        source = quality.Source;
                    }
                }

                if (resolution == Resolution.R2160p)
                {
					result.ResolutionDetectionSource = QualityDetectionSource.Name;
					
                	if (codecRegex.Groups["x265"].Success)
                	{
                    	result.Quality = Quality.HDTV2160pHEVC;
                    	return result;
                	}

                    result.Quality = source == QualitySource.Unknown
                        ? Quality.HDTV2160p
                        : QualityFinder.FindBySourceAndResolution(source, 2160);

                    return result;
                }

                if (resolution == Resolution.R1080p)
                {
					result.ResolutionDetectionSource = QualityDetectionSource.Name;
                	if (codecRegex.Groups["x265"].Success)
                	{
                    	result.Quality = Quality.HDTV1080pHEVC;
                    	return result;
                	}
                    result.Quality = source == QualitySource.Unknown
                    ? Quality.HDTV1080p
                    : QualityFinder.FindBySourceAndResolution(source, 1080);

                    return result;
                }

                if (resolution == Resolution.R720p)
                {
					result.ResolutionDetectionSource = QualityDetectionSource.Name;
                	if (codecRegex.Groups["x265"].Success)
                	{
                    	result.Quality = Quality.HDTV720pHEVC;
                    	return result;
                	}
					result.Quality = source == QualitySource.Unknown
                        ? Quality.HDTV720p
                        : QualityFinder.FindBySourceAndResolution(source, 720);

                    return result;
                }

                if (resolution == Resolution.R360P || resolution == Resolution.R480P)
                {
                    result.ResolutionDetectionSource = QualityDetectionSource.Name;

                    result.Quality = source == QualitySource.Unknown
                        ? Quality.SDTV
                        : QualityFinder.FindBySourceAndResolution(source, 480);

                    return result;
                }
            }


            if (codecRegex.Groups["x264"].Success)
            {
                result.Quality = Quality.SDTV;
                
                return result;
            }

            if (normalizedName.Contains("848x480"))
            {
                result.ResolutionDetectionSource = QualityDetectionSource.Name;
                
                if (normalizedName.Contains("dvd"))
                {
                    result.SourceDetectionSource = QualityDetectionSource.Name;
                    result.Quality = Quality.DVD;
                }
                else if (normalizedName.ContainsIgnoreCase("bluray"))
                {
                    result.SourceDetectionSource = QualityDetectionSource.Name;
                    result.Quality = Quality.Bluray480p;
                }
                else
                {
                    result.Quality = Quality.SDTV;
                }

                return result;
            }

            if (normalizedName.ContainsIgnoreCase("1280x720"))
            {
                result.ResolutionDetectionSource = QualityDetectionSource.Name;
                
                if (normalizedName.ContainsIgnoreCase("bluray"))
                {
                    result.SourceDetectionSource = QualityDetectionSource.Name;
                    result.Quality = Quality.Bluray720p;
                }
                else
                {
                    result.Quality = Quality.HDTV720p;
                }

                return result;
            }

            if (normalizedName.ContainsIgnoreCase("1920x1080"))
            {
                result.ResolutionDetectionSource = QualityDetectionSource.Name;
                
                if (normalizedName.ContainsIgnoreCase("bluray"))
                {
                    result.SourceDetectionSource = QualityDetectionSource.Name;
                    result.Quality = Quality.Bluray1080p;
                }
                else
                {
                    result.Quality = Quality.HDTV1080p;
                }

                return result;
            }

            if (normalizedName.ContainsIgnoreCase("bluray720p"))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.ResolutionDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Bluray720p;

                return result;
            }

            if (normalizedName.ContainsIgnoreCase("bluray1080p"))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.ResolutionDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Bluray1080p;

                return result;
            }

            if (normalizedName.ContainsIgnoreCase("bluray2160p"))
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.ResolutionDetectionSource = QualityDetectionSource.Name;
                result.Quality = Quality.Bluray2160p;

                return result;
            }

            var otherSourceMatch = OtherSourceMatch(normalizedName);

            if (otherSourceMatch != Quality.Unknown)
            {
                result.SourceDetectionSource = QualityDetectionSource.Name;
                result.Quality = otherSourceMatch;
            }

            return result;
        }

        private static Resolution ParseResolution(string name)
        {
            var match = ResolutionRegex.Match(name);

            if (!match.Success) return Resolution.Unknown;
            if (match.Groups["R360p"].Success) return Resolution.R360P;
            if (match.Groups["R480p"].Success) return Resolution.R480P;
            if (match.Groups["R576p"].Success) return Resolution.R576p;
            if (match.Groups["R720p"].Success) return Resolution.R720p;
            if (match.Groups["R1080p"].Success) return Resolution.R1080p;
            if (match.Groups["R2160p"].Success) return Resolution.R2160p;

            return Resolution.Unknown;
        }

        private static Quality OtherSourceMatch(string name)
        {
            var match = OtherSourceRegex.Match(name);

            if (!match.Success) return Quality.Unknown;
            if (match.Groups["sdtv"].Success) return Quality.SDTV;
            if (match.Groups["hdtv"].Success) return Quality.HDTV720p;

            return Quality.Unknown;
        }

        private static QualityModel ParseQualityModifiers(string name, string normalizedName)
        {
            var result = new QualityModel { Quality = Quality.Unknown };

            if (ProperRegex.IsMatch(normalizedName))
            {
                result.Revision.Version = 2;
            }

            if (RepackRegex.IsMatch(normalizedName))
            {
                result.Revision.Version = 2;
                result.Revision.IsRepack = true;
            }

            var versionRegexResult = VersionRegex.Match(normalizedName);

            if (versionRegexResult.Success)
            {
                result.Revision.Version = Convert.ToInt32(versionRegexResult.Groups["version"].Value);
            }

            //TODO: re-enable this when we have a reliable way to determine real
            //TODO: Only treat it as a real if it comes AFTER the season/epsiode number
            var realRegexResult = RealRegex.Matches(name);

            if (realRegexResult.Count > 0)
            {
                result.Revision.Real = realRegexResult.Count;
            }

            return result;
        }
    }

    public enum Resolution
    {
        R360P,
        R480P,
        R576p,
        R720p,
        R1080p,
        R2160p,
        Unknown
    }
}
