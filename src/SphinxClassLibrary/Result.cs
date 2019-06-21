namespace SphinxClassLibrary
{
    using System.Collections.Generic;
    using System.Text;
    using SphinxClassLibrary.IO;


    /// <summary>
    /// 
    /// </summary>
    public class WordInfo
    {
        public string Word { get; private set; }
        public int Docs { get; private set; }
        public int Hits { get; private set; }

        public override string ToString()
        {
            return string.Format("Word={0} Docs={1} Hits={2}", Word, Docs, Hits);
        }

        internal void ReadFrom(SphinxBinaryReader _br)
        {
            Word = _br.ReadStr();
            Docs = _br.ReadInt();
            Hits = _br.ReadInt();
        }
    }

    /// <summary>
    /// A result match
    /// </summary>
    public class Match
    {
        public long Id { get; private set; }
        public int Weight { get; private set; }
        public Dictionary<string, AttributeValue> Attributes = new Dictionary<string, AttributeValue>();

        public Match(long _id)
        {
            Id = _id;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Id={0} Weight={1}", Id, Weight));
            foreach (KeyValuePair<string, AttributeValue> av in Attributes)
                sb.AppendLine("\t" + av.Value);
            return sb.ToString();
        }

        internal void ReadFrom(SphinxBinaryReader _br, IEnumerable<Attribute> _attributes)
        {
            Weight = _br.ReadInt();
            foreach (Attribute attr in _attributes)
            {
                AttributeValue attr_val = new AttributeValue(attr);
                attr_val.ReadFrom(_br);
                Attributes.Add(attr.Name, attr_val);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Result
    {
        public string Error { get; private set; }
        public string Warning { get; private set; }
        public SeachdStatusCodes Status { get; private set; }
        public int Total { get; private set; }
        public int TotalFound { get; private set; }
        public int TimeMsec { get; private set; }
        public IEnumerable<string> Fields { get { return FieldList; } }
        public IEnumerable<Match> Matches { get { return MatcheList; } }
        public IEnumerable<WordInfo> Words{ get { return WordList; } }
        public IEnumerable<Attribute> Attributes { get { return AttributeList; } }

        private List<string> FieldList = new List<string>();
        private List<Match> MatcheList = new List<Match>();
        private List<WordInfo> WordList = new List<WordInfo>();
        private List<Attribute> AttributeList = new List<Attribute>();


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Error=" + Error);
            sb.AppendLine("Warning=" + Warning);
            sb.AppendLine("Status=" + Status.ToString());
            sb.AppendLine("Total=" + Total);
            sb.AppendLine("TotalFound=" + TotalFound);
            sb.AppendLine("TimeMsec=" + TimeMsec);
            sb.AppendLine("Fields=" + FieldList.Count);
            foreach (string f in FieldList)
                sb.AppendLine("\t" + f);
            sb.AppendLine("Matches=" + MatcheList.Count);
            foreach (Match m in MatcheList)
                sb.AppendLine("\t" + m);
            sb.AppendLine("Words=" + WordList.Count);
            foreach (WordInfo wi in WordList)
                sb.AppendLine("\t" + wi);
            return sb.ToString();
        }

        internal void ReadFrom(SphinxBinaryReader _br)
        {
            Status = (SeachdStatusCodes)_br.ReadInt();

            if (Status != SeachdStatusCodes.SEARCHD_OK)
            {
                if (Status == SeachdStatusCodes.SEARCHD_WARNING)
                    Warning = _br.ReadStr();
                else
                {
                    Error = _br.ReadStr();
                    return;
                }
            }
            
            int count = _br.ReadInt(); // num_fields
            while (count-- > 0)
                FieldList.Add(_br.ReadStr());

            count = _br.ReadInt(); // num_attr
            while (count-- > 0)
                AttributeList.Add(new Attribute(_br.ReadStr(), (AttributeTypes)_br.ReadInt()));

            count = _br.ReadInt(); // num_matches
            bool id64 = _br.ReadInt() == 1;
            while (count-- > 0)
            {
                Match m = new Match((id64) ? _br.ReadLong() : _br.ReadInt());
                m.ReadFrom(_br, Attributes);
                MatcheList.Add(m);
            }

            Total = _br.ReadInt();
            TotalFound = _br.ReadInt();
            TimeMsec = _br.ReadInt();

            count = _br.ReadInt(); // num_words
            while (count-- > 0)
            {
                WordInfo wi = new WordInfo();
                wi.ReadFrom(_br);
                WordList.Add(wi);
            }
        }
    }
}
