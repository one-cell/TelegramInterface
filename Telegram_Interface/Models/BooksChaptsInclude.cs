using System;
using System.Collections.Generic;
using System.Text;

namespace Telegram_Interface.Models
{
    public class BookChaptsInclude
    {
        public string Book { get; set; }
        public string BookId { get; set; }
        public string BibleId { get; set; }
        public string Abbreviation { get; set; }
        public string Name { get; set; }
        public string NameLong { get; set; }
        public List<string> Chapters { get; set; }
    }
}
