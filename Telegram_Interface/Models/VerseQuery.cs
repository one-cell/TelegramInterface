using System;
using System.Collections.Generic;

namespace Bible_Bot.Models
{
    public class VerseQuery
    {
        public QueryVersesData Data { get; set; }
    }

    public class QueryVersesData
    {
        public string Total { get; set; }
        public List<QueryVerseItem> Verses { get; set; }

    }
    public class QueryVerseItem
    {
        public string Id { get; set; }
        public string BookId { get; set; }
        public string BibleId { get; set; }
        public string ChapterId { get; set; }
        public string Reference { get; set; }
        public string Text { get; set; }
    }
}