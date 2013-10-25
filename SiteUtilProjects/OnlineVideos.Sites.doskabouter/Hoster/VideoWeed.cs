﻿using System;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class VideoWeed : MyHosterBase
    {
        public override string getHosterUrl()
        {
            return "Videoweed.es";
        }

        public override string getVideoUrls(string url)
        {
            string page = SiteUtilBase.GetWebData(url);
            if (!string.IsNullOrEmpty(page))
            {
                string step1 = WiseCrack(page);

                string link = FlashProvider(step1);
                if (!String.IsNullOrEmpty(link))
                    videoType = VideoType.flv;
                return link;
            }
            return String.Empty;
        }
    }
}
