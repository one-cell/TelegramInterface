using System;
using System.Collections.Generic;
using System.Text;

namespace Telegram_Interface.Models
{
    class BibleDb
    {
        public string BibleId { get; set; }
        public string Name { get; set; }
        public string NameLocal { get; set; }
        public string Abbreviation { get; set; }
        public string AbbreviationLocal { get; set; }
        public string Language { get; set; }
    }
}
