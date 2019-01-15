﻿using HtmlAgilityPack;
using OnlineVideos.Hoster;
using OnlineVideos.Subtitles;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class DreamfilmHDUtil : SiteUtilBase
    {
        protected enum SubtitleSource
        {
            None,
            OpenSubtitles,
            Podnapisi,
            Subscene,
            TvSubtitles
        }

        protected enum Language
        {
            eng,
            swe
        }

        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        protected string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Token used for vkpass video urls")]
        protected string vkpassToken;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Backup subtitle source"), Description("Try to download subtiles from this subtitle source if no subtitles from hoster")]
        protected SubtitleSource subtitleSource = SubtitleSource.None;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Backup subtitle source language"), Description("Select subtitle language preferences")]
        protected Language subtitleLanguage = Language.swe;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Always use the backup subtitle source"), Description("Always use the backup subtitle source")]
        protected bool forceSubtitle = false;

        private SubtitleHandler sh = null;
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            sh = new SubtitleHandler(subtitleSource == SubtitleSource.None ? "" : subtitleSource.ToString(), subtitleLanguage.ToString());
        }
        public override int DiscoverDynamicCategories()
        {
            string data = GetWebData(baseUrl, encoding: Encoding.UTF8);
            Regex rgx = new Regex(@"'list-group-item\s'><a\shref='(?<url>[^']*)'>(?<title>[^<]*)");
            foreach(Match m in rgx.Matches(data))
            {
                Settings.Categories.Add(new RssLink() { Name = m.Groups["title"].Value, Url = m.Groups["url"].Value});
            }
            foreach (Category c in Settings.Categories) c.HasSubCategories = true;
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 2;
            return Settings.Categories.Count;
        }

        private List<Category> GetSubCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            string data = GetWebData((parentCategory as RssLink).Url, encoding: Encoding.UTF8);
            Regex rgx = new Regex(@"data-content="".*?/>(?<description>[^""]*).*?href=""(?<url>[^""]*)""><b>(?<title>[^<]*).*?src=""(?<thumb>[^""]*)", RegexOptions.Singleline);
            foreach (Match m in rgx.Matches(data))
            {
                string thumb = m.Groups["thumb"].Value;
                if (!thumb.StartsWith("http"))
                    thumb = baseUrl + thumb;
                cats.Add(new RssLink() { Name = m.Groups["title"].Value, Url = m.Groups["url"].Value, Thumb = thumb, Description = m.Groups["description"].Value, ParentCategory = parentCategory });
            }
            rgx = new Regex(@"href='(?<url>[^']*)'>&raquo;");
            Match match = rgx.Match(data);
            if (match.Success)
            {
                cats.Add(new NextPageCategory() { Url = match.Groups["url"].Value, ParentCategory = parentCategory });
            }
            return cats;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = GetSubCategories(parentCategory);
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            List<Category> nextPageCats = GetSubCategories(category);
            foreach(Category c in nextPageCats) c.ParentCategory = category.ParentCategory;
            category.ParentCategory.SubCategories.Remove(category);
            category.ParentCategory.SubCategories.AddRange(nextPageCats);
            return nextPageCats.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = (category as RssLink).Url;
            if (url.Contains("/series/"))
            {
                HtmlNode doc = GetWebData<HtmlDocument>(url, encoding: Encoding.UTF8).DocumentNode;
                HtmlNodeCollection seasons = doc.SelectNodes("//div[contains(@class,'season') and contains(@class ,'panel')]");
                foreach(HtmlNode season in seasons)
                {
                    string seasonId = season.GetAttributeValue("id", "");
                    Regex rgx = new Regex(@"s(\d+)");
                    Match m = rgx.Match(seasonId);
                    uint seasonNo = 0;
                    if (m.Success)
                    {
                        seasonNo = uint.Parse(m.Groups[1].Value);
                    }
                    HtmlNodeCollection episodes = season.SelectNodes(".//a[contains(@class,'episode')]");
                    foreach(HtmlNode episode in episodes)
                    {
                        string episodeName = episode.InnerText;
                        rgx = new Regex(@"[^\d]*(\d+)");
                        m = rgx.Match(episodeName);
                        uint episodeNo = 0;
                        if (m.Success)
                        {
                            episodeNo = uint.Parse(m.Groups[1].Value);
                        }
                        string href = episode.GetAttributeValue("href", "");
                        rgx = new Regex(@",\s*?'(?<url>[^']*)");
                        m = rgx.Match(href);
                        if (m.Success)
                        {
                            href = GetIframeUrl(m.Groups["url"].Value);
                        }
                        ITrackingInfo ti = new TrackingInfo() { Title = category.Name, VideoKind = VideoKind.TvSeries, Season = seasonNo, Episode = episodeNo };
                        videos.Add(new VideoInfo()
                            {
                                Title = category.Name + " " + (seasonNo > 9 ? seasonNo.ToString() : ("0" + seasonNo.ToString())) + "x" + (episodeNo > 9 ? episodeNo.ToString() : ("0" + episodeNo.ToString())),
                                Other = ti,
                                Description = episodeName,
                                Thumb = category.Thumb,
                                VideoUrl = href
                            });
                    }
                }
            }
            else
            {
                string data = GetWebData(url, encoding: Encoding.UTF8);
                ITrackingInfo ti = new TrackingInfo() { Title = category.Name, VideoKind = VideoKind.Movie };
                Regex rgx = new Regex(@"http://www.imdb.com/title/(tt[^/]*)");
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    ti.ID_IMDB = m.Groups[1].Value;
                }

                rgx = new Regex(@"<li class=""t\s.*?id=""(?<tabId>[^""]*).*?href=""#"">(?<tabName>[^<]*)");
                foreach(Match tabMatch in rgx.Matches(data))
                {
                    string tabName = tabMatch.Groups["tabName"].Value;
                    string tabId = tabMatch.Groups["tabId"].Value;
                    Regex rgxATab = new Regex(@"<div class=""movbox.*?id=""m" + tabId + @""".*?write\(a\('(?<iframe>[^']*)", RegexOptions.Singleline);
                    Match matchATab = rgxATab.Match(data);
                    if (matchATab.Success)
                    {
                        string videoUrl = GetIframeUrl(matchATab.Groups["iframe"].Value);
                        if (!string.IsNullOrEmpty(videoUrl))
                        {
                            videos.Add(new VideoInfo()
                            {
                                Description = category.Description,
                                Other = ti,
                                Thumb = category.Thumb,
                                Title = category.Name + " - (" + tabName + ")",
                                VideoUrl = videoUrl
                            });
                        }
                    }
                }
            }
            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<Hoster.HosterBase> hosters = Hoster.HosterFactory.GetAllHosters();
            string url = video.VideoUrl; ;
            video.PlaybackOptions = new Dictionary<string, string>();
            url = url.Replace("ok.ru/", string.Format("vkpass.com/token/{0}/", vkpassToken));
            Hoster.HosterBase hoster = hosters.FirstOrDefault(h => url.ToLower().Contains(h.GetHosterUrl().ToLower()));
            if (hoster != null)
            {
                if (hoster is IReferer)
                    (hoster as IReferer).RefererUrl = baseUrl;
                Dictionary<string, string> hosterPo = hoster.GetPlaybackOptions(url);
                if (hosterPo != null)
                {
                    foreach (string key in hosterPo.Keys)
                    {
                        if (!string.IsNullOrEmpty(hosterPo[key]))
                            video.PlaybackOptions.Add((hoster.GetType().Name != key ? hoster.GetType().Name + " " : "") + key, hosterPo[key]);
                    }
                }
                if (!forceSubtitle && hoster is ISubtitle)
                    video.SubtitleText = (hoster as ISubtitle).SubtitleText;
            }
            else
            {
                Log.Debug("Dreamfilm, no hoster found for url: {0}", url);
            }
            url = video.PlaybackOptions.Count == 0 ? "" : video.PlaybackOptions.FirstOrDefault().Value;
            if (inPlaylist)
                video.PlaybackOptions.Clear();
            if (!string.IsNullOrWhiteSpace(url) && subtitleSource != SubtitleSource.None)
                sh.SetSubtitleText(video, GetTrackingInfo, false);

            return new List<string>() { url };
        }

        private string GetIframeUrl(string encodedIframe)
        {
            string url = null;
            encodedIframe = System.Text.Encoding.ASCII.GetString(System.Convert.FromBase64String(encodedIframe));
            encodedIframe = HttpUtility.UrlDecode(encodedIframe);
            if (encodedIframe.StartsWith("//"))
                url = "http:" + encodedIframe;
            else if (encodedIframe.StartsWith("http"))
            {
                url = encodedIframe;
            }
            else
            {
                Regex rgx = new Regex(@"src=""(?<url>[^""]*)");
                Match match = rgx.Match(encodedIframe);
                if (match.Success)
                {
                    url = match.Groups["url"].Value;
                }
                else
                {
                    rgx = new Regex(@"src='(?<url>[^']*)");
                    match = rgx.Match(encodedIframe);
                    if (match.Success)
                    {
                        url = match.Groups["url"].Value;
                    }
                }
            }
            return url;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video.Other is ITrackingInfo)
                return video.Other as ITrackingInfo;
            return base.GetTrackingInfo(video);
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> searchResults = new List<SearchResultItem>();
            List<Category> catResults =  GetSubCategories(new RssLink() { Url = string.Format("{0}search/?q={1}", baseUrl, HttpUtility.UrlEncode(query)) });
            catResults.ForEach(c => searchResults.Add(c));
            return searchResults;
        }
    }
}
