namespace SphinxClassLibrary
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Sockets;
    using SphinxClassLibrary.IO;
    using SphinxClassLibrary.Net;


    public class SphinxClient
    {
        public const int MAX_REQ = 32;
        public const int MAX_PACKET_LEN = 8 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the last error.
        /// </summary>
        /// <value>The last error.</value>
        public string LastError { get; set; }

        /// <summary>
        /// Gets or sets the last warning.
        /// </summary>
        /// <value>The last warning.</value>
        public string LastWarning { get; set; }

        /// <summary>
        /// Default "localhost"
        /// </summary>
        /// <value>The host.</value>
        public string Host { get; set; }

        /// <summary>
        /// Default 9312
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }

        /// <summary>
        /// Query matching mode. Default SPH_MATCH_ALL
        /// </summary>
        /// <value>The match mode.</value>
        public MatchModes Mode { get; set; }

        /// <summary>
        /// Match sorting mode. Default SPH_SORT_RELEVANCE
        /// </summary>
        /// <value>The sort mode.</value>
        public SortModes Sort { get; set; }

        /// <summary>
        /// Gets or sets the sort by.
        /// attribute to sort by
        /// </summary>
        /// <value>The sort by attribute.</value>
        public string SortBy { get; set; }

        /// <summary>
        /// Gets or sets the group function. Default SPH_GROUPBY_ATTR
        /// </summary>
        /// <value>The group function.</value>
        public GroupingFunctions GroupFunction { get; set; }

        /// <summary>
        /// Gets or sets the group by.
        /// </summary>
        /// <value>group-by attribute name.</value>
        public string GroupBy { get; set; }

        /// <summary>
        /// Gets or sets the group sort. Default "@group desc"
        /// </summary>
        /// <value>group-by sorting clause.</value>
        public string GroupSort { get; set; }

        /// <summary>
        /// Gets or sets the group distinct.
        /// </summary>
        /// <value>group-by count-distinct attribute.</value>
        public string GroupDistinct { get; set; }

        /// <summary>
        /// Gets or sets the ranker. Default SPH_RANK_PROXIMITY_BM25
        /// </summary>
        /// <value>The ranker.</value>
        public RankingModes Ranker { get; set; }

        /// <summary>
        /// Min ID to match. Default 0 (no limit)
        /// </summary>
        /// <value>The min id.</value>
        public ulong MinId { get; set; }

        /// <summary>
        /// Max ID to match. Default 0 (no limit)
        /// </summary>
        /// <value>The max id.</value>
        public ulong MaxId { get; set; }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>The offset.</value>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets the limit. Default 20
        /// </summary>
        /// <value>The limit.</value>
        public int Limit { get; set; }

        /// <summary>
        /// Gets or sets the max matches. Default 1000
        /// </summary>
        /// <value>The max matches.</value>
        public int MaxMatches { get; set; }

        /// <summary>
        /// Gets or sets the cutoff.
        /// cutoff to stop searching at (default is 0)
        /// </summary>
        /// <value>The cutoff.</value>
        public int Cutoff { get; set; }

        /// <summary>
        /// Gets or sets the retry count.
        /// distributed retries count
        /// </summary>
        /// <value>The retry count.</value>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the retry delay.
        /// distributed retries delay
        /// </summary>
        /// <value>The retry delay.</value>
        public int RetryDelay { get; set; }

        /// <summary>
        /// Gets or sets the max query time in Milliseconds
        /// </summary>
        /// <value>The max query time.</value>
        public int MaxQueryTime { get; set; }

        /// <summary>
        /// Gets or sets the select list. Default "*"
        /// attributes or expressions, with optional aliases
        /// </summary>
        /// <value>The select list.</value>
        public string SelectList { get; set; }

        // geo anchor
        public string AnchorLatitudeAttr { get; protected set; }
        public string AnchorLongitudeAttr { get; protected set; }
        public float AnchorLatitude { get; protected set; }
        public float AnchorLongitude { get; protected set; }

        /// <summary>
        /// per index weight
        /// </summary>
        public Dictionary<string, int> IndexWeights = new Dictionary<string, int>();

        /// <summary>
        /// per field weight
        /// </summary>
        public Dictionary<string, int> FieldsWeights = new Dictionary<string, int>();

        /// <summary>
        /// char encoding, default "UTF-8"
        /// </summary>
        public string Encoding { get; set; }

        private List<Stream> queries = new List<Stream>();
        private List<Filter> filters = new List<Filter>();


        public SphinxClient(string _host = "localhost", int _port = 9312)
        {
            Host = _host;
            Port = _port;
            Mode = MatchModes.SPH_MATCH_ALL;
            Sort = SortModes.SPH_SORT_RELEVANCE;
            GroupFunction = GroupingFunctions.SPH_GROUPBY_ATTR;
            Ranker = RankingModes.SPH_RANK_PROXIMITY_BM25;
            GroupSort = "@group desc";
            MaxMatches = 1000;
            SelectList = "*";
            Limit = 20;
            Encoding = "UTF-8";
        }

        /// <summary>
        /// Shortcut for AddQuery followed by a RunQueries
        /// </summary>
        /// <param name="_query">The _query.</param>
        /// <param name="_index">The _index.</param>
        /// <param name="_comment">The _comment.</param>
        /// <returns></returns>
        public Result Query(string _query, string _index = "", string _comment = "")
        {
            if (queries.Count != 0)
                throw new SphinxClientException("There are queries to be processed (" + queries.Count + ")");
            AddQuery(_query, _index, _comment);
            return RunQueries()[0];
        }

        /// <summary>
        /// Adds the query.
        /// </summary>
        /// <param name="_query">The _query.</param>
        /// <param name="_index">The _index.</param>
        /// <param name="_comment">The _comment.</param>
        /// <returns>queries count</returns>
        public int AddQuery(string _query, string _index = "", string _comment = "")
        {
            if (queries.Count == MAX_REQ)
                throw new SphinxClientException("Too many queries (Max " + MAX_REQ + ")");

            MemoryStream query_stream = new MemoryStream();
            SphinxBinaryWriter bw = new SphinxBinaryWriter(query_stream, Encoding);
            bw.WriteInt(Offset); // offset
            bw.WriteInt(Limit); // limit
            bw.WriteInt((int)Mode); // mode
            bw.WriteInt((int)Ranker); // ranker
            bw.WriteInt((int)Sort); // sort
            bw.WriteStr(SortBy); // sortby
            bw.WriteStr(_query); // query
            bw.WriteInt(0); // weights (deprecated)
            bw.WriteStr(_index); // index
            bw.WriteInt(1); // id64 range
            bw.WriteLong((long)MinId); // minid
            bw.WriteLong((long)MaxId); // maxid
            // filters
            bw.WriteInt(filters.Count);
            foreach (Filter sf in filters)
                sf.WriteTo(bw);
            bw.WriteInt((int)GroupFunction); // group
            bw.WriteStr(GroupBy); // groupby
            bw.WriteInt(MaxMatches); // max matches
            bw.WriteStr(GroupSort); // group sort
            bw.WriteInt(Cutoff); // cutoff
            bw.WriteInt(RetryCount); // retrycount
            bw.WriteInt(RetryDelay); // retrydelay
            bw.WriteStr(GroupDistinct); // groupdistinct
            // geoanchor
            if ((AnchorLatitudeAttr != null) && (AnchorLongitudeAttr != null))
            {
                bw.WriteInt(1);
                bw.WriteStr(AnchorLatitudeAttr);
                bw.WriteStr(AnchorLongitudeAttr);
                bw.WriteFloat(AnchorLatitude);
                bw.WriteFloat(AnchorLongitude);
            }
            else
                bw.WriteInt(0);
            // index weights
            bw.WriteInt(IndexWeights.Count);
            foreach (KeyValuePair<string, int> iw in IndexWeights)
            {
                bw.WriteStr(iw.Key);
                bw.WriteInt(iw.Value);
            }
            bw.WriteInt(MaxQueryTime); // maxquerytime
            // per-field weights
            bw.WriteInt(FieldsWeights.Count);
            foreach (KeyValuePair<string, int> fw in FieldsWeights)
            {
                bw.WriteStr(fw.Key);
                bw.WriteInt(fw.Value);
            }
            bw.WriteStr(_comment); // comment
            bw.WriteInt(0); // attribute overrides TODO

            bw.WriteStr(SelectList); // select-list

            bw.Flush();

            queries.Add(query_stream);
            return queries.Count;
        }

        /// <summary>
        /// Runs all queries.
        /// </summary>
        /// <returns>a list of SphinxResult</returns>
        public List<Result> RunQueries()
        {
            SphinxBinaryWriter req_bw = new SphinxBinaryWriter(new MemoryStream(), Encoding);
            // prepare header
            req_bw.WriteShort((short)SearchdCommand.SEARCHD_COMMAND_SEARCH);
            req_bw.WriteShort((short)VerCommand.VER_COMMAND_SEARCH);
            int req_len = 8;
            foreach (Stream s in queries)
                req_len += (int)s.Length;
            req_bw.WriteInt(req_len);
            req_bw.WriteInt(0);
            req_bw.WriteInt(queries.Count);
            // prepare all queries
            foreach (Stream s in queries)
                req_bw.WriteStream(s);
            req_bw.Flush();

            using (SphinxConnection sc = GetConnection())
            {
                SphinxBinaryWriter client_bw = new SphinxBinaryWriter(sc.Stream, Encoding);
                client_bw.WriteStream(req_bw.Stream);

                SphinxBinaryReader res_br = new SphinxBinaryReader(ReadResponse(sc.Stream), Encoding);
                List<Result> result = new List<Result>();
                for (int i = 0; i < queries.Count; i++)
                {
                    Result sr = new Result();
                    sr.ReadFrom(res_br);
                    result.Add(sr);
                }
                queries.Clear(); // clear all queries
                return result;
            }
        }

        /// <summary>
        /// Builds excerpts.
        /// </summary>
        /// <param name="_docs">The _docs.</param>
        /// <param name="_index">The _index.</param>
        /// <param name="_words">The _words.</param>
        /// <param name="_opts">The _opts.</param>
        /// <returns></returns>
        public List<string> BuildExcerpts(List<string> _docs, string _index, string _words, ExcerptOptions _opts)
        {
            SphinxBinaryWriter req_bw = new SphinxBinaryWriter(new MemoryStream(), Encoding);
            req_bw.WriteShort((short)SearchdCommand.SEARCHD_COMMAND_EXCERPT);
            req_bw.WriteShort((short)VerCommand.VER_COMMAND_EXCERPT);
            req_bw.WriteInt(0); // request length
            req_bw.WriteInt(0); // mode=0
            req_bw.WriteInt((int)_opts.Flags); // flags
            req_bw.WriteStr(_index); // index
            req_bw.WriteStr(_words); // words
            _opts.WriteTo(req_bw); // ExcerptOptions
            // docs
            req_bw.WriteInt(_docs.Count);
            foreach (string d in _docs)
                req_bw.WriteStr(d);
            req_bw.Flush();

            req_bw.Seek(4, SeekOrigin.Begin); // move to the request length position
            req_bw.WriteInt((int)req_bw.Stream.Length - 8); // request length - 8 (fixed header)
            req_bw.Flush();

            using (SphinxConnection sc = GetConnection())
            {
                SphinxBinaryWriter client_bw = new SphinxBinaryWriter(sc.Stream, Encoding);
                client_bw.WriteStream(req_bw.Stream);

                SphinxBinaryReader res_br = new SphinxBinaryReader(ReadResponse(sc.Stream), Encoding);
                List<string> results = new List<string>();
                for (int i = 0; i < _docs.Count; i++)
                    results.Add(res_br.ReadStr());
                return results;
            }
        }

        /// <summary>
        /// Builds keywords.
        /// </summary>
        /// <param name="_query">The _query.</param>
        /// <param name="_index">The _index.</param>
        /// <param name="_hits">if set to <c>true</c> [_hits].</param>
        /// <returns></returns>
        public List<Keyword> BuildKeywords(string _query, string _index = "", bool _hits = true)
        {
            SphinxBinaryWriter req_bw = new SphinxBinaryWriter(new MemoryStream(), Encoding);
            req_bw.WriteShort((short)SearchdCommand.SEARCHD_COMMAND_KEYWORDS);
            req_bw.WriteShort((short)VerCommand.VER_COMMAND_KEYWORDS);
            req_bw.WriteInt(12 + _query.Length + _index.Length);
            req_bw.WriteStr(_query);
            req_bw.WriteStr(_index);
            req_bw.WriteInt(_hits ? 1 : 0);
            req_bw.Flush();

            using (SphinxConnection sc = GetConnection())
            {
                SphinxBinaryWriter client_bw = new SphinxBinaryWriter(sc.Stream, Encoding);
                client_bw.WriteStream(req_bw.Stream);

                SphinxBinaryReader res_br = new SphinxBinaryReader(ReadResponse(sc.Stream), Encoding);
                int num_keywords = res_br.ReadInt();
                List<Keyword> results = new List<Keyword>();
                while (num_keywords-- > 0)
                {
                    Keyword kw = new Keyword();
                    kw.Tokenized = res_br.ReadStr();
                    kw.Normalized = res_br.ReadStr();
                    if (_hits)
                    {
                        kw.NumDocs = res_br.ReadInt();
                        kw.NumHits = res_br.ReadInt();
                    }
                    results.Add(kw);
                }
                return results;
            }
        }

        /// <summary>
        /// Returns a list of statuses
        /// </summary>
        /// <returns></returns>
        public List<string> Status()
        {
            SphinxBinaryWriter req_bw = new SphinxBinaryWriter(new MemoryStream(), Encoding);
            req_bw.WriteShort((short)SearchdCommand.SEARCHD_COMMAND_STATUS);
            req_bw.WriteShort((short)VerCommand.VER_COMMAND_STATUS);
            req_bw.WriteInt(4);
            req_bw.WriteInt(1);
            req_bw.Flush();

            using (SphinxConnection sc = GetConnection())
            {
                SphinxBinaryWriter client_bw = new SphinxBinaryWriter(sc.Stream, Encoding);
                client_bw.WriteStream(req_bw.Stream);

                SphinxBinaryReader res_br = new SphinxBinaryReader(ReadResponse(sc.Stream), Encoding);
                int num_rows = res_br.ReadInt();
                int num_cols = res_br.ReadInt();
                List<string> results = new List<string>();
                for (int i = 0; i < num_rows; i++)
                    for (int j = 0; j < num_cols; j++)
                        results.Add(res_br.ReadStr());
                return results;
            }
        }

        /// <summary>
        /// Forces searchd to flush pending attribute updates to disk, and blocks until completion.
        /// </summary>
        /// <returns>Returns a non-negative internal "flush tag"</returns>
        public int FlushAttributes()
        {
            SphinxBinaryWriter req_bw = new SphinxBinaryWriter(new MemoryStream(), Encoding);
            req_bw.WriteShort((short)SearchdCommand.SEARCHD_COMMAND_FLUSHATTRS);
            req_bw.WriteShort((short)VerCommand.VER_COMMAND_FLUSHATTRS);
            req_bw.WriteInt(0);
            req_bw.Flush();

            using (SphinxConnection sc = GetConnection())
            {
                SphinxBinaryWriter client_bw = new SphinxBinaryWriter(sc.Stream, Encoding);
                client_bw.WriteStream(req_bw.Stream);

                SphinxBinaryReader res_br = new SphinxBinaryReader(ReadResponse(sc.Stream), Encoding);
                return res_br.ReadInt();
            }
        }

        public int UpdateAttributes(string _index, List<Attribute> _attrs, Dictionary<long, List<AttributeValue>> _values)
        {
            SphinxBinaryWriter req_bw = new SphinxBinaryWriter(new MemoryStream(), Encoding);
            req_bw.WriteShort((short)SearchdCommand.SEARCHD_COMMAND_UPDATE);
            req_bw.WriteShort((short)VerCommand.VER_COMMAND_UPDATE);
            req_bw.WriteInt(0); // request length
            req_bw.WriteStr(_index);
            req_bw.WriteInt(_attrs.Count);
            foreach (Attribute a in _attrs)
            {
                req_bw.WriteStr(a.Name);
                req_bw.WriteInt(a.isMVA ? 1 : 0);
            }
            req_bw.WriteInt(_values.Count);
            foreach (KeyValuePair<long, List<AttributeValue>> kv in _values)
            {
                req_bw.WriteLong(kv.Key);
                foreach (AttributeValue av in kv.Value)
                    av.WriteTo(req_bw);
            }
            req_bw.Flush();

            req_bw.Seek(4, SeekOrigin.Begin); // move to the request length position
            req_bw.WriteInt((int)req_bw.Stream.Length - 8); // request length - 8 (fixed header)
            req_bw.Flush();

            using (SphinxConnection sc = GetConnection())
            {
                SphinxBinaryWriter client_bw = new SphinxBinaryWriter(sc.Stream, Encoding);
                client_bw.WriteStream(req_bw.Stream);

                SphinxBinaryReader res_br = new SphinxBinaryReader(ReadResponse(sc.Stream), Encoding);
                return res_br.ReadInt();
            }
        }

        private SphinxConnection GetConnection()
        {
            return new SphinxConnection(Host, Port);
        }

        /// <summary>
        /// Reads the response from a _tcpclient
        /// This method can set LastError or LastWarning
        /// </summary>
        /// <param name="_tcpclient">A connected TcpClient.</param>
        /// <returns></returns>
        private Stream ReadResponse(NetworkStream _stream)
        {
            LastError = "";
            LastWarning = "";
            Stream ms = null;

            // read response header (8 bytes)
            SphinxBinaryReader client_br = new SphinxBinaryReader(_stream, Encoding);
            SphinxBinaryReader res_br = new SphinxBinaryReader(new MemoryStream(client_br.ReadBytes(8)), Encoding);
            SeachdStatusCodes res_status = (SeachdStatusCodes)res_br.ReadShort();
            res_br.ReadShort();
            int res_len = res_br.ReadInt();
            if ((res_len < 0) || (res_len > MAX_PACKET_LEN))
                throw new SphinxClientException(string.Format("response length out of bounds (len={0})", res_len));

            // read response (res_len)
            res_br = new SphinxBinaryReader(new MemoryStream(client_br.ReadBytes(res_len)), Encoding);
            switch (res_status)
            {
                case SeachdStatusCodes.SEARCHD_OK:
                    ms = res_br.Stream;
                    break;
                case SeachdStatusCodes.SEARCHD_ERROR:
                case SeachdStatusCodes.SEARCHD_RETRY:
                    LastError = res_br.ReadStr();
                    throw new SphinxClientException(LastError);
                case SeachdStatusCodes.SEARCHD_WARNING:
                    LastWarning = res_br.ReadStr();
                    ms = res_br.Stream;
                    break;
                default:
                    throw new SphinxClientException(string.Format("unknown status (status={0})", res_status));
            }

            return ms;
        }

        public void ResetFilters()
        {
            filters.Clear();
        }

        public void AddFilter(string _attr, long[] _values, bool _exclude)
        {
            filters.Add(new FilterValues(_attr, _exclude, _values));
        }

        public void AddFilterRange(string _attr, long _min, long _max, bool _exclude)
        {
            filters.Add(new FilterRange(_attr, _exclude, _min, _max));
        }

        public void AddFilterFloatRange(string _attr, float _min, float _max, bool _exclude)
        {
            filters.Add(new FilterRangeFloat(_attr, _exclude, _min, _max));
        }

        public void ResetGeoAnchors()
        {
            AnchorLatitudeAttr = "";
            AnchorLongitudeAttr = "";
            AnchorLatitude = 0;
            AnchorLongitude = 0;
        }

        public void SetGeoAnchors(string _attrLat, string _attrLong, float _lat, float _long)
        {
            AnchorLatitudeAttr = _attrLat;
            AnchorLongitudeAttr = _attrLong;
            AnchorLatitude = _lat;
            AnchorLongitude = _long;
        }
    }
}
