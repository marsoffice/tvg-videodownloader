﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MarsOffice.Tvg.VideoDownloader.Abstractions
{
    public class VideoBackgroundResult
    {
        public string VideoId { get; set; }
        public string JobId { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string FileLink { get; set; }
        public string FileName { get; set; }
        public string Category { get; set; }
        public string LanguageCode { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}
