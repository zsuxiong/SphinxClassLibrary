namespace SphinxClassLibrary
{
    public class ExcerptOptions
    {
        public string BeforeMatch { get; set; }
        public string AfterMatch { get; set; }
        public string ChunkSeparator { get; set; }
        public string HtmlStripMode { get; set; }
        public string PassageBoundary { get; set; }

        public int Limit { get; set; }
        public int LimitPassages { get; set; }
        public int LimitWords { get; set; }
        public int Around { get; set; }
        public int StartPassageId { get; set; }

        public ExcerptFlags Flags { get; set; }

        public ExcerptOptions()
        {
            BeforeMatch = "<b>";
            AfterMatch = "</b>";
            ChunkSeparator = " ... ";
            HtmlStripMode = "index";
            PassageBoundary = "none";
            Limit = 256;
            LimitPassages = 0;
            LimitWords = 0;
            Around = 5;
            StartPassageId = 1;
            Flags = ExcerptFlags.REMOVE_SPACES;
        }

        internal void WriteTo(IO.SphinxBinaryWriter _bw)
        {
            _bw.WriteStr(BeforeMatch); // before_match
            _bw.WriteStr(AfterMatch); // after_match
            _bw.WriteStr(ChunkSeparator); // chunk_separator
            _bw.WriteInt(Limit); // limit
            _bw.WriteInt(Around); // around
            _bw.WriteInt(LimitPassages); // limit_passages
            _bw.WriteInt(LimitWords); // limit_words
            _bw.WriteInt(StartPassageId); // start_passage_id
            _bw.WriteStr(HtmlStripMode); // html_strip_mode
            _bw.WriteStr(PassageBoundary); // passage_boundary
        }
    }
}
