using System;
using System.Collections.Generic;

namespace Bible_Bot.Models
{
    public class BiblesPublic
    {
        public List<Bible> Data { get; set; }

    }
    public class Bible
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NameLocal { get; set; }
        public string Abbreviation { get; set; }
        public string AbbreviationLocal { get; set; }
        public Language Language { get; set; }
    }
    public class Language
    {
        public string Id { get; set; }
    }
}