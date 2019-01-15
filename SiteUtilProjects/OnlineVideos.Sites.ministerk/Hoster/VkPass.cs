﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class VkPass : HosterBase, ISubtitle, IReferer
    {
        private string subtitleText = null;
        private string refererUrl = null;

        public override string GetHosterUrl()
        {
            return "vkpass.com";
        }

        private Dictionary<string, string> GetPlaybackOptionsFromIFrame(string data, string refUrl)
        {
            List<Hoster.HosterBase> hosters = Hoster.HosterFactory.GetAllHosters();

            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            Regex rgx = new Regex(@"<iframe[^>]*?src='(?<url>[^']*)");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                string url = m.Groups["url"].Value;
                Hoster.HosterBase hoster = hosters.FirstOrDefault(h => url.ToLower().StartsWith("http://" + h.GetHosterUrl().ToLower()));
                if (hoster != null)
                {
                    if (hoster is IReferer)
                        (hoster as IReferer).RefererUrl = refUrl;
                    playbackOptions = hoster.GetPlaybackOptions(url);
                }
            }
            return playbackOptions;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            subtitleText = null;
            string refUrl = RefererUrl;
            //Clear referer
            RefererUrl = null;

            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            try
            {
                string data = GetWebData(url, referer: refUrl);
                playbackOptions = GetPlaybackOptionsFromIFrame(data, refUrl);
                if (playbackOptions.Count < 1)
                {
                    Regex rgx = new Regex(@"changeSource\('(?<source>[^']*)");
                    foreach (Match sMatch in rgx.Matches(data))
                    {
                        if (sMatch.Success)
                        {
                            bool qOrAmp = url.Contains("?");
                            string source = sMatch.Groups["source"].Value;
                            string sUrl = url + (qOrAmp ? "&" : "?") + "source=" + source;
                            Dictionary<string, string> tmpPbos = GetPlaybackOptionsFromIFrame(GetWebData(sUrl, referer: refUrl), refUrl);
                            if (tmpPbos.Count > 0)
                            {
                                foreach (KeyValuePair<string, string> kvp in tmpPbos)
                                {
                                    playbackOptions.Add(kvp.Key + " " + source, kvp.Value);
                                }
                            }
                        }
                    }
                }

                string subUrl = "";

                Regex regex = new Regex(@"(?<sub>http[^&]*?.\.vtt)");
                Match m = regex.Match(url);
                if (m.Success)
                {
                    subUrl = m.Groups["sub"].Value;
                }
                if (!string.IsNullOrWhiteSpace(subUrl))
                {
                    try
                    {
                        subtitleText = "";
                        string tmpTxt = WebCache.Instance.GetWebData(subUrl, forceUTF8: true);
                        tmpTxt = Regex.Replace(tmpTxt, @"[^\u0000-\u00FF]", "").Replace("\r", "");
                        Regex rgx = new Regex(@"(?<text>\d\d:\d\d:\d\d\.\d\d\d\s*?-->\s*?\d\d:\d\d:\d\d\.\d\d\d\n.*?\n\n)", RegexOptions.Singleline);
                        int i = 0;
                        foreach (Match match in rgx.Matches(tmpTxt))
                        {
                            i++;
                            subtitleText += i + "\n" + match.Groups["text"].Value;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return playbackOptions;
        }

        public override string GetVideoUrl(string url)
        {
            Dictionary<string, string> urls = GetPlaybackOptions(url);
            if (urls.Count > 0)
                return urls.First().Value;
            else
                return "";
        }


        public string SubtitleText
        {
            get { return subtitleText; }
        }

        public string RefererUrl
        {
            get
            {
                return refererUrl;
            }
            set
            {
                refererUrl = value;
            }
        }
    }
}