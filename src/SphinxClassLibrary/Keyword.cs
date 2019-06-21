namespace SphinxClassLibrary
{
    public class Keyword
    {
        public string Tokenized { get; set; }
        public string Normalized { get; set; }
        public int NumDocs { get; set; }
        public int NumHits { get; set; }

        public override string ToString()
        {
            return string.Format("Tokenized={0} Normalized={1} NumDocs={2} NumHits={3}", Tokenized, Normalized, NumDocs, NumHits);
        }
    }
}
