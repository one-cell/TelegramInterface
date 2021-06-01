using System;
using System.Collections.Generic;
using System.Text;

namespace Telegram_Interface.Models
{
    public class ChapterResp
    {
        public string Chapter { get; set; }
        public string ChapterId { get; set; }
        public string BibleId { get; set; }
        public string BookId { get; set; }
        public string Number { get; set; }
        public string Reference { get; set; }
        public int VerseCount { get; set; }
        public string Content { get; set; }

    }
}
