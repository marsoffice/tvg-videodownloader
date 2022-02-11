using System;
using System.Collections.Generic;
using System.Text;

namespace MarsOffice.Tvg.VideoDownloader.Abstractions
{
    public class RequestVideoBackground
    {
        public string VideoId { get; set; }
        public string JobId { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string Category { get; set; }
        public string LanguageCode { get; set; }
    }
}
