using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ConsoleApp1.Test
{

    public class SphinxClient : IDisposable
    {
        #region Static Values
        /* matching modes */
        public static int SPH_MATCH_ALL = 0;
        public static int SPH_MATCH_ANY = 1;
        public static int SPH_MATCH_PHRASE = 2;
        public static int SPH_MATCH_BOOLEAN = 3;
        public static int SPH_MATCH_EXTENDED = 4;
        public static int SPH_MATCH_FULLSCAN = 5;
        public static int SPH_MATCH_EXTENDED2 = 6;

        /* sorting modes */
        public static int SPH_SORT_RELEVANCE = 0;
        public static int SPH_SORT_ATTR_DESC = 1;
        public static int SPH_SORT_ATTR_ASC = 2;
        public static int SPH_SORT_TIME_SEGMENTS = 3;
        public static int SPH_SORT_EXTENDED = 4;
        public static int SPH_SORT_EXPR = 5;

        /* grouping functions */
        public static int SPH_GROUPBY_DAY = 0;
        public static int SPH_GROUPBY_WEEK = 1;
        public static int SPH_GROUPBY_MONTH = 2;
        public static int SPH_GROUPBY_YEAR = 3;
        public static int SPH_GROUPBY_ATTR = 4;
        public static int SPH_GROUPBY_ATTRPAIR = 5;

        /* searchd reply status codes */
        public static int SEARCHD_OK = 0;
        public static int SEARCHD_ERROR = 1;
        public static int SEARCHD_RETRY = 2;
        public static int SEARCHD_WARNING = 3;

        /* attribute types */
        public static int SPH_ATTR_INTEGER = 1;
        public static int SPH_ATTR_TIMESTAMP = 2;
        public static int SPH_ATTR_ORDINAL = 3;
        public static int SPH_ATTR_BOOL = 4;
        public static int SPH_ATTR_FLOAT = 5;
        public static int SPH_ATTR_BIGINT = 6;
        public static int SPH_ATTR_MULTI = 0x40000000;


        /* searchd commands */
        private static int SEARCHD_COMMAND_SEARCH = 0;
        private static int SEARCHD_COMMAND_EXCERPT = 1;
        private static int SEARCHD_COMMAND_UPDATE = 2;
        private static int SEARCHD_COMMAND_KEYWORDS = 3;
        private static int SEARCHD_COMMAND_PERSIST = 4;
        private static int SEARCHD_COMMAND_STATUS = 5;
        private static int SEARCHD_COMMAND_QUERY = 6;

        /* searchd command versions */
        private static int VER_COMMAND_SEARCH = 0x116;
        private static int VER_COMMAND_EXCERPT = 0x100;
        private static int VER_COMMAND_UPDATE = 0x102;
        private static int VER_COMMAND_KEYWORDS = 0x100;
        private static int VER_COMMAND_STATUS = 0x100;
        private static int VER_COMMAND_QUERY = 0x100;

        /* filter types */
        private static int SPH_FILTER_VALUES = 0;
        private static int SPH_FILTER_RANGE = 1;
        private static int SPH_FILTER_FLOATRANGE = 2;

        private static int SPH_CLIENT_TIMEOUT_MILLISEC = 0;

        #endregion

        #region Variable Declaration

        private string _host;
        private int _port;
        private int _offset;
        private int _limit;
        private int _mode;
        private int[] _weights;
        private int _sort;
        private string _sortby;
        private long _minId;
        private long _maxId;
        private int _filterCount;
        private string _groupBy;
        private int _groupFunc;
        private string _groupSort;
        private string _groupDistinct;
        private int _maxMatches;
        private int _cutoff;
        private int _retrycount;
        private int _retrydelay;
        private string _latitudeAttr;
        private string _longitudeAttr;
        private float _latitude;
        private float _longitude;
        private string _error;
        private string _warning;
        private Dictionary<string, int> _fieldWeights;

        private TcpClient _conn;

        // request queries already created
        List<byte[]> _requestQueries = new List<byte[]>();

        private Dictionary<string, int> _indexWeights;

        // use a memorystream instead of a byte array because it's easier to augment
        MemoryStream _filterStreamData = new MemoryStream();

        #endregion

        #region Constructors

        /**
     * Creates new SphinxClient instance.
     *
     * Default host and port that the instance will connect to are
     * localhost:3312. That can be overriden using {@link #SetServer SetServer()}.
     */
       

        /**
         * Creates new SphinxClient instance, with host:port specification.
         *
         * Host and port can be later overriden using {@link #SetServer SetServer()}.
         *
         * @param host searchd host name (default: localhost)
         * @param port searchd port number (default: 3312)
         */
        public SphinxClient(string host, int port)
        {
            _host = host;
            _port = port;
            _offset = 0;
            _limit = 20;
            _mode = SPH_MATCH_ALL;
            _sort = SPH_SORT_RELEVANCE;
            _sortby = "";
            _minId = 0;
            _maxId = 0;

            _filterCount = 0;

            _groupBy = "";
            _groupFunc = SPH_GROUPBY_DAY;
            // _groupSort = "@group desc";
            _groupSort = "";
            _groupDistinct = "";

            _maxMatches = 1000;
            _cutoff = 0;
            _retrycount = 0;
            _retrydelay = 0;

            _latitudeAttr = null;
            _longitudeAttr = null;
            _latitude = 0;
            _longitude = 0;

            _error = "";
            _warning = "";

            //_reqs         = new ArrayList();
            _weights = null;
            _indexWeights = new Dictionary<string, int>();
            _fieldWeights = new Dictionary<string, int>();

        }

        #endregion

        #region Main Functions
        /** Connect to searchd server and run current search query against all indexes (syntax sugar). */
        public SphinxResult Query(string query)
        {
            return Query(query, "*");
        }

        /**
         * Connect to searchd server and run current search query.
         *
         * @param query     query string
         * @param index     index name(s) to query. May contain anything-separated
         *                  list of index names, or "*" which means to query all indexes.
         * @return          {@link SphinxResult} object
         *
         * @throws SphinxException on invalid parameters
         */
        public SphinxResult Query(string query, string index)
        {
            //MyAssert(_requestQueries == null || _requestQueries.Count == 0, "AddQuery() and Query() can not be combined; use RunQueries() instead");

            AddQuery(query, index);
            SphinxResult[] results = RunQueries();
            if (results == null || results.Length < 1)
            {
                return null; /* probably network error; error message should be already filled */
            }

            SphinxResult res = results[0];
            _warning = res.warning;
            _error = res.error;
            return res;
        }

        public int AddQuery(string query, string index)
        {
            byte[] outputdata = new byte[2048];

            /* build request */
            try
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter sw = new BinaryWriter(ms);

                WriteToStream(sw, _offset);
                WriteToStream(sw, _limit);
                WriteToStream(sw, _mode);
                WriteToStream(sw, 0); //SPH_RANK_PROXIMITY_BM25
                WriteToStream(sw, _sort);

                WriteToStream(sw, _sortby);
                WriteToStream(sw, query);

                //_weights = new int[] { 100, 1 };
                _weights = null;

                int weightLen = _weights != null ? _weights.Length : 0;

                WriteToStream(sw, weightLen);
                if (_weights != null)
                {
                    for (int i = 0; i < _weights.Length; i++)
                        WriteToStream(sw, _weights[i]);
                }

                WriteToStream(sw, index);
                WriteToStream(sw, 1); // id64
                WriteToStream(sw, _minId);
                WriteToStream(sw, _maxId);

                /* filters */
                WriteToStream(sw, _filterCount);
                if (_filterCount > 0 && _filterStreamData.Length > 0)
                {
                    byte[] filterdata = new byte[_filterStreamData.Length];
                    _filterStreamData.Seek(0, SeekOrigin.Begin);
                    _filterStreamData.Read(filterdata, 0, (int)_filterStreamData.Length);
                    WriteToStream(sw, filterdata);
                }

                /* group-by, max matches, sort-by-group flag */
                WriteToStream(sw, _groupFunc);
                WriteToStream(sw, _groupBy);
                WriteToStream(sw, _maxMatches);
                WriteToStream(sw, _groupSort);

                WriteToStream(sw, _cutoff);
                WriteToStream(sw, _retrycount);
                WriteToStream(sw, _retrydelay);

                WriteToStream(sw, _groupDistinct);

                /* anchor point */
                if (_latitudeAttr == null || _latitudeAttr.Length == 0 || _longitudeAttr == null || _longitudeAttr.Length == 0)
                {
                    WriteToStream(sw, 0);
                }
                else
                {
                    WriteToStream(sw, 1);
                    WriteToStream(sw, _latitudeAttr);
                    WriteToStream(sw, _longitudeAttr);
                    WriteToStream(sw, _latitude);
                    WriteToStream(sw, _longitude);
                }

                /* per-index weights */
                //sw.Write(_indexWeights.size());
                WriteToStream(sw, this._indexWeights.Count);
                foreach (KeyValuePair<string, int> item in this._indexWeights)
                {
                    WriteToStream(sw, item.Key);
                    WriteToStream(sw, item.Value);
                }

                // max query time
                WriteToStream(sw, 0);
                // per-field weights
                WriteToStream(sw, this._fieldWeights.Count);
                foreach (KeyValuePair<string, int> item in this._fieldWeights)
                {
                    WriteToStream(sw, item.Key);
                    WriteToStream(sw, item.Value);
                }
                // comment
                WriteToStream(sw, "");
                // attribute overrides
                WriteToStream(sw, 0);
                // select-list
                WriteToStream(sw, "*");
                sw.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                byte[] data = new byte[ms.Length];
                ms.Read(data, 0, (int)ms.Length);

                int qIndex = _requestQueries.Count;
                _requestQueries.Add(data);

                return qIndex;

            }
            catch (Exception ex)
            {
                //MyAssert(false, "error on AddQuery: " + ex.Message);
            }
            return -1;
        }

        /** Run all previously added search queries. */
        public SphinxResult[] RunQueries()
        {
            if (_requestQueries == null || _requestQueries.Count < 1)
            {
                _error = "no queries defined, issue AddQuery() first";
                return null;
            }

            if (Conn == null) return null;

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            /* send query, get response */
            int nreqs = _requestQueries.Count;
            try
            {

                WriteToStream(bw, (short)SEARCHD_COMMAND_SEARCH);
                WriteToStream(bw, (short)VER_COMMAND_SEARCH);

                //return null;
                int rqLen = 4;
                for (int i = 0; i < nreqs; i++)
                {
                    byte[] subRq = (byte[])_requestQueries[i];
                    rqLen += subRq.Length;
                }
                WriteToStream(bw, rqLen);
                WriteToStream(bw, nreqs);

                for (int i = 0; i < nreqs; i++)
                {
                    byte[] subRq = (byte[])_requestQueries[i];
                    WriteToStream(bw, subRq);
                }
                ms.Flush();
                byte[] buffer = new byte[ms.Length];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);
                bw = new BinaryWriter(Conn.GetStream());
                bw.Write(buffer, 0, buffer.Length);
                bw.Flush();
                bw.BaseStream.Flush();
                ms.Close();
            }
            catch (Exception e)
            {
                //MyAssert(false, "Query: Unable to create read/write streams: " + e.Message);
                return null;
            }

            /* get response */
            byte[] response = GetResponse(Conn, VER_COMMAND_SEARCH);

            /* parse response */
            SphinxResult[] results = ParseResponse(response);

            /* reset requests */
            _requestQueries = new List<byte[]>();

            return results;
        }

        private SphinxResult[] ParseResponse(byte[] response)
        {
            if (response == null) return null;

            /* parse response */
            SphinxResult[] results = new SphinxResult[_requestQueries.Count];

            BinaryReader br = new BinaryReader(new MemoryStream(response));

            /* read schema */
            int ires;
            try
            {
                for (ires = 0; ires < _requestQueries.Count; ires++)
                {
                    SphinxResult res = new SphinxResult();
                    results[ires] = res;

                    int status = ReadInt32(br);
                    res.setStatus(status);
                    if (status != SEARCHD_OK)
                    {
                        string message = ReadUtf8(br);
                        if (status == SEARCHD_WARNING)
                        {
                            res.warning = message;
                        }
                        else
                        {
                            res.error = message;
                            continue;
                        }
                    }

                    /* read fields */
                    int nfields = ReadInt32(br);
                    res.fields = new string[nfields];
                    //int pos = 0;
                    for (int i = 0; i < nfields; i++)
                        res.fields[i] = ReadUtf8(br);

                    /* read arrts */
                    int nattrs = ReadInt32(br);
                    res.attrTypes = new int[nattrs];
                    res.attrNames = new string[nattrs];
                    for (int i = 0; i < nattrs; i++)
                    {
                        string AttrName = ReadUtf8(br);
                        int AttrType = ReadInt32(br);
                        res.attrNames[i] = AttrName;
                        res.attrTypes[i] = AttrType;
                    }

                    /* read match count */
                    int count = ReadInt32(br);
                    int id64 = ReadInt32(br);
                    res.matches = new SphinxMatch[count];
                    for (int matchesNo = 0; matchesNo < count; matchesNo++)
                    {
                        SphinxMatch docInfo;
                        docInfo = new SphinxMatch(
                                (id64 == 0) ? ReadUInt32(br) : ReadInt64(br),
                                ReadInt32(br));

                        /* read matches */
                        for (int attrNumber = 0; attrNumber < res.attrTypes.Length; attrNumber++)
                        {
                            string attrName = res.attrNames[attrNumber];
                            int type = res.attrTypes[attrNumber];

                            /* handle bigint */
                            if (type == SPH_ATTR_BIGINT)
                            {
                                docInfo.attrValues.Add(ReadInt64(br));
                                continue;
                            }

                            /* handle floats */
                            if (type == SPH_ATTR_FLOAT)
                            {
                                docInfo.attrValues.Add(ReadFloat(br));
                                //docInfo.attrValues.add ( attrNumber, bw.ReadDouble  ) );
                                //throw new NotImplementedException("we don't read floats yet");
                                continue;
                            }

                            /* handle everything else as unsigned ints */
                            long val = ReadUInt32(br);
                            if ((type & SPH_ATTR_MULTI) != 0)
                            {
                                long[] vals = new long[(int)val];
                                for (int k = 0; k < val; k++)
                                    vals[k] = ReadUInt32(br);

                                docInfo.attrValues.Add(vals);

                            }
                            else
                            {
                                docInfo.attrValues.Add(val);
                            }
                        }
                        res.matches[matchesNo] = docInfo;
                    }

                    res.total = ReadInt32(br);
                    res.totalFound = ReadInt32(br);
                    res.time = ReadInt32(br) / 1000.0f; /*  format is %.3f */
                    int wordsCount = ReadInt32(br);

                    //res.words = new SphinxWordInfo[ReadInt32(bw)];
                    //for (int i = 0; i < res.words.Length; i++)
                    //    res.words[i] = new SphinxWordInfo(ReadUtf8(bw), ReadUInt32(bw), ReadUInt32(bw));
                }

                br.Close();
                return results;

            }
            catch (IOException e)
            {
                //MyAssert(false, "unable to parse response: " + e.Message);
                return null;
            }
        }

        private TcpClient Conn
        {
            get
            {
                try
                {
                    if (_conn == null || !_conn.Connected)
                    {
                        _conn = new TcpClient(_host, _port);

                        NetworkStream ns = _conn.GetStream();
                        BinaryReader sr = new BinaryReader(ns);
                        BinaryWriter sw = new BinaryWriter(ns);

                        // check the version.
                        WriteToStream(sw, 1);
                        sw.Flush();
                        int version = 0;
                        version = ReadInt32(sr);

                        if (version < 1)
                        {
                            _conn.Close();
                            // "expected searchd protocol version 1+, got version " + version;
                            _conn = null;
                            return null;
                        }

                        // set persist connect
                        WriteToStream(sw, (short)4); // COMMAND_Persist
                        WriteToStream(sw, (short)0); //PERSIST_COMMAND_VERSION
                        WriteToStream(sw, 4); // COMMAND_LENGTH
                        WriteToStream(sw, 1); // PERSIST_COMMAND_BODY
                        sw.Flush();
                    }
                }
                catch (IOException e)
                {
                    try
                    {
                        _conn.Close();
                    }
                    catch
                    {
                        _conn = null;
                    }
                    return null;
                }
                return _conn;
            }
        }


        #endregion

        #region Getters and Setters
        /**
     * Get last error message, if any.
     *
     * @return string with last error message (empty string if no errors occured)
     */
        public string GetLastError()
        {
            return _error;
        }

        /**
         * Get last warning message, if any.
         *
         * @return string with last warning message (empty string if no errors occured)
         */
        public string GetLastWarning()
        {
            return _warning;
        }

        /**
         * Set searchd host and port to connect to.
         *
         * @param host searchd host name (default: localhost)
         * @param port searchd port number (default: 3312)
         *
         * @throws SphinxException on invalid parameters
         */
        public void SetServer(string host, int port)
        {
            //MyAssert(host != null && host.Length > 0, "host name must not be empty");
            //MyAssert(port > 0 && port < 65536, "port must be in 1..65535 range");
            _host = host;
            _port = port;
        }

        /** Set matches offset and limit to return to client, max matches to retrieve on server, and cutoff. */
        public void SetLimits(int offset, int limit, int max, int cutoff)
        {
            //MyAssert(offset >= 0, "offset must be greater than or equal to 0");
            //MyAssert(limit > 0, "limit must be greater than 0");
            //MyAssert(max > 0, "max must be greater than 0");
            //MyAssert(cutoff >= 0, "max must be greater than or equal to 0");

            _offset = offset;
            _limit = limit;
            _maxMatches = max;
            _cutoff = cutoff;
        }

        /** Set matches offset and limit to return to client, and max matches to retrieve on server. */
        public void SetLimits(int offset, int limit, int max)
        {
            SetLimits(offset, limit, max, _cutoff);
        }

        /** Set matches offset and limit to return to client. */
        public void SetLimits(int offset, int limit)
        {
            SetLimits(offset, limit, _maxMatches, _cutoff);
        }

        /** Set matching mode. */
        public void SetMatchMode(int mode)
        {
            //MyAssert(
            //    mode == SPH_MATCH_ALL ||
            //    mode == SPH_MATCH_ANY ||
            //    mode == SPH_MATCH_PHRASE ||
            //    mode == SPH_MATCH_BOOLEAN ||
            //    mode == SPH_MATCH_EXTENDED, "unknown mode value; use one of the available SPH_MATCH_xxx constants");
            _mode = mode;
        }

        /** Set sorting mode. */
        public void SetSortMode(int mode, string sortby)
        {
            //MyAssert(
            //    mode == SPH_SORT_RELEVANCE ||
            //    mode == SPH_SORT_ATTR_DESC ||
            //    mode == SPH_SORT_ATTR_ASC ||
            //    mode == SPH_SORT_TIME_SEGMENTS ||
            //    mode == SPH_SORT_EXTENDED, "unknown mode value; use one of the available SPH_SORT_xxx constants");
            //MyAssert(mode == SPH_SORT_RELEVANCE || (sortby != null && sortby.Length > 0), "sortby string must not be empty in selected mode");

            _sort = mode;
            _sortby = (sortby == null) ? "" : sortby;
        }

        /** Set per-field weights (all values must be positive). */
        public void SetWeights(int[] weights)
        {
            //MyAssert(weights != null, "weights must not be null");
            for (int i = 0; i < weights.Length; i++)
            {
                int weight = weights[i];
                //MyAssert(weight > 0, "all weights must be greater than 0");
            }
            _weights = weights;
        }

        public void SetFieldWeights(string field, int weight)
        {
            if (this._fieldWeights.ContainsKey(field)) this._fieldWeights[field] = weight;
            else this._fieldWeights.Add(field, weight);
        }

        /**
         * Set per-index weights
         *
         * @param indexWeights hash which maps string index names to Integer weights
         */
        public void SetIndexWeights(string index, int weight)
        {
            if (this._indexWeights.ContainsKey(index)) this._indexWeights[index] = weight;
            else this._indexWeights.Add(index, weight);
        }

        /**
         * Set document IDs range to match.
         *
         * Only match those documents where document ID is beetwen given
         * min and max values (including themselves).
         *
         * @param min minimum document ID to match
         * @param max maximum document ID to match
         *
         * @throws SphinxException on invalid parameters
         */
        public void SetIDRange(int min, int max)
        {
            //MyAssert(min <= max, "min must be less or equal to max");
            _minId = min;
            _maxId = max;
        }

        /**
         * Set values filter.
         *
         * Only match those documents where <code>attribute</code> column value
         * is in given values set.
         *
         * @param attribute     attribute name to filter by
         * @param values        values set to match the attribute value by
         * @param exclude       whether to exclude matching documents instead
         *
         * @throws SphinxException on invalid parameters
         */
        public void SetFilter(string attribute, long[] values, bool exclude)
        {
            //MyAssert(values != null && values.Length > 0, "values array must not be null or empty");
            //MyAssert(attribute != null && attribute.Length > 0, "attribute name must not be null or empty");
            if (values == null || values.Length == 0) return;

            try
            {
                BinaryWriter bw = new BinaryWriter(_filterStreamData);

                WriteToStream(bw, attribute);
                WriteToStream(bw, SPH_FILTER_VALUES);
                WriteToStream(bw, values.Length);

                for (int i = 0; i < values.Length; i++)
                    WriteToStream(bw, values[i]);

                WriteToStream(bw, exclude ? 1 : 0);

            }
            catch (Exception e)
            {
                //MyAssert(false, "IOException: " + e.Message);
            }
            _filterCount++;
        }

        public void SetFilter(string attribute, int[] values, bool exclude)
        {
            //MyAssert(values != null && values.Length > 0, "values array must not be null or empty");
            //MyAssert(attribute != null && attribute.Length > 0, "attribute name must not be null or empty");
            if (values == null || values.Length == 0) return;
            long[] v = new long[values.Length];
            for (int i = 0; i < values.Length; i++) v[i] = (long)values[i];
            SetFilter(attribute, v, exclude);
        }

        /** Set values filter with a single value (syntax sugar; see {@link #SetFilter(string,int[],bool)}). */
        public void SetFilter(string attribute, long value, bool exclude)
        {
            long[] values = new long[] { value };
            SetFilter(attribute, values, exclude);
        }

        /** Set values filter with a single value (syntax sugar; see {@link #SetFilter(string,int[],bool)}). */
        public void SetFilter(string attribute, int value, bool exclude)
        {
            long[] values = new long[] { value };
            SetFilter(attribute, values, exclude);
        }

        public void SetFilter(string attribute, bool value, bool exclude)
        {
            SetFilter(attribute, value ? 1 : 0, exclude);
        }

        public void SetFilter(string attribute, DateTime value, bool exclude)
        {
            SetFilter(attribute, ConvertToUnixTimestamp(value), exclude);
        }

        public void SetFilter(string attribute, DateTime[] values, bool exclude)
        {
            if (values == null || values.Length == 0) return;
            int[] items = new int[values.Length];
            for (int i = 0; i < items.Length; i++) items[i] = ConvertToUnixTimestamp(values[i]);
            SetFilter(attribute, items, exclude);
        }

        /**
         * Set integer range filter.
         *
         * Only match those documents where <code>attribute</code> column value
         * is beetwen given min and max values (including themselves).
         *
         * @param attribute     attribute name to filter by
         * @param min           min attribute value
         * @param max           max attribute value
         * @param exclude       whether to exclude matching documents instead
         *
         * @throws SphinxException on invalid parameters
         */
        public void SetFilterRange(string attribute, int min, int max, bool exclude)
        {
            SetFilterRange(attribute, (long)min, (long)max, exclude);
        }

        public void SetFilterRange(string attribute, DateTime min, DateTime max, bool exclude)
        {
            SetFilterRange(attribute, ConvertToUnixTimestamp(min), ConvertToUnixTimestamp(max), exclude);
        }

        public void SetFilterRange(string attribute, long min, long max, bool exclude)
        {
            //MyAssert(min <= max, "min must be less or equal to max");
            try
            {
                BinaryWriter bw = new BinaryWriter(_filterStreamData);
                WriteToStream(bw, attribute);
                WriteToStream(bw, SPH_FILTER_RANGE);
                WriteToStream(bw, min);
                WriteToStream(bw, max);
                WriteToStream(bw, exclude ? 1 : 0);

            }
            catch (Exception e)
            {
                //MyAssert(false, "IOException: " + e.Message);
            }
            _filterCount++;
        }

        /**
         * Set float range filter.
         *
         * Only match those documents where <code>attribute</code> column value
         * is beetwen given min and max values (including themselves).
         *
         * @param attribute     attribute name to filter by
         * @param min           min attribute value
         * @param max           max attribute value
         * @param exclude       whether to exclude matching documents instead
         *
         * @throws SphinxException on invalid parameters
         * Set float range filter.
         */
        public void SetFilterFloatRange(string attribute, float min, float max, bool exclude)
        {
            //MyAssert(min <= max, "min must be less or equal to max");
            try
            {
                BinaryWriter bw = new BinaryWriter(_filterStreamData);
                WriteToStream(bw, attribute);
                WriteToStream(bw, SPH_FILTER_FLOATRANGE);
                WriteToStream(bw, min);
                WriteToStream(bw, max);
                WriteToStream(bw, exclude ? 1 : 0);

            }
            catch (Exception e)
            {
                //MyAssert(false, "IOException: " + e.Message);
            }
            _filterCount++;
        }

        /** Reset all currently set filters (for multi-queries). */
        public void ResetFilters()
        {
            /* should we close them first? */
            _filterStreamData = new MemoryStream();
            _filterCount = 0;

            /* reset GEO anchor */
            _latitudeAttr = null;
            _longitudeAttr = null;
            _latitude = 0;
            _longitude = 0;
        }

        /**
         * Setup geographical anchor point.
         *
         * Required to use @geodist in filters and sorting.
         * Distance will be computed to this point.
         *
         * @param latitudeAttr      the name of latitude attribute
         * @param longitudeAttr     the name of longitude attribute
         * @param latitude          anchor point latitude, in radians
         * @param longitude         anchor point longitude, in radians
         *
         * @throws SphinxException on invalid parameters
         */
        public void SetGeoAnchor(string latitudeAttr, string longitudeAttr, float latitude, float longitude)
        {
            //MyAssert(latitudeAttr != null && latitudeAttr.Length > 0, "longitudeAttr string must not be null or empty");
            //MyAssert(longitudeAttr != null && longitudeAttr.Length > 0, "longitudeAttr string must not be null or empty");

            _latitudeAttr = latitudeAttr;
            _longitudeAttr = longitudeAttr;
            _latitude = latitude;
            _longitude = longitude;
        }

        /** Set grouping attribute and function. */
        public void SetGroupBy(string attribute, int func, string groupsort)
        {
            //MyAssert(
            //    func == SPH_GROUPBY_DAY ||
            //    func == SPH_GROUPBY_WEEK ||
            //    func == SPH_GROUPBY_MONTH ||
            //    func == SPH_GROUPBY_YEAR ||
            //    func == SPH_GROUPBY_ATTR ||
            //    func == SPH_GROUPBY_ATTRPAIR, "unknown func value; use one of the available SPH_GROUPBY_xxx constants");

            _groupBy = attribute;
            _groupFunc = func;
            _groupSort = groupsort;
        }

        /** Set grouping attribute and function with default ("@group desc") groupsort (syntax sugar). */
        public void SetGroupBy(string attribute, int func)
        {
            SetGroupBy(attribute, func, "@group desc");
        }

        /** Set count-distinct attribute for group-by queries. */
        public void SetGroupDistinct(string attribute)
        {
            _groupDistinct = attribute;
        }

        /** Set distributed retries count and delay. */
        public void SetRetries(int count, int delay)
        {
            //MyAssert(count >= 0, "count must not be negative");
            //MyAssert(delay >= 0, "delay must not be negative");
            _retrycount = count;
            _retrydelay = delay;
        }

        /** Set distributed retries count with default (zero) delay (syntax sugar). */
        public void SetRetries(int count)
        {
            SetRetries(count, 0);
        }


        #endregion

        #region Private Methods

        /** Get and check response packet from searchd (internal method). */
        private byte[] GetResponse(TcpClient sock, int client_ver)
        {
            /* connect */
            BinaryReader br = null;
            NetworkStream SockInput = null;
            try
            {
                SockInput = sock.GetStream();
                br = new BinaryReader(SockInput);
            }
            catch (IOException e)
            {
                //MyAssert(false, "getInputStream() failed: " + e.Message);
                return null;
            }

            /* read response */
            byte[] response = null;
            int status = 0, ver = 0;
            int len = 0;
            try
            {
                /* read status fields */
                status = ReadInt16(br);
                ver = ReadInt16(br);
                len = ReadInt32(br);

                /* read response if non-empty */
                //MyAssert(len > 0, "zero-sized searchd response body");
                if (len > 0)
                {
                    response = br.ReadBytes(len);
                }
                else
                {
                    /* FIXME! no response, return null? */
                }

                /* check status */
                if (status == SEARCHD_WARNING)
                {
                    //DataInputStream in = new DataInputStream ( new ByteArrayInputStream ( response ) );

                    //int iWarnLen = in.ReadInt32 ();
                    //_warning = new string ( response, 4, iWarnLen );

                    //System.arraycopy ( response, 4+iWarnLen, response, 0, response.Length-4-iWarnLen );
                    _error = "searchd warning";
                    return null;

                }
                else if (status == SEARCHD_ERROR)
                {
                    _error = "searchd error: " + Encoding.UTF8.GetString(response, 4, response.Length - 4);
                    return null;

                }
                else if (status == SEARCHD_RETRY)
                {
                    _error = "temporary searchd error: " + Encoding.UTF8.GetString(response, 4, response.Length - 4);
                    return null;

                }
                else if (status != SEARCHD_OK)
                {
                    _error = "searched returned unknown status, code=" + status;
                    return null;
                }

            }
            catch (IOException e)
            {
                if (len != 0)
                {
                    /* get trace, to provide even more failure details */
                    //PrintWriter ew = new PrintWriter ( new StringWriter() );
                    //e.printStackTrace ( ew );
                    //ew.flush ();
                    //ew.close ();
                    //string sTrace = ew.toString ();

                    /* build error message */
                    _error = "failed to read searchd response (status=" + status + ", ver=" + ver + ", len=" + len + ", trace=" + e.StackTrace + ")";
                }
                else
                {
                    _error = "received zero-sized searchd response (searchd crashed?): " + e.Message;
                }
                return null;

            }
            finally
            {
                try
                {
                    if (br != null) br.Close();
                    if (sock != null && !sock.Connected) sock.Close();
                }
                catch (IOException e)
                {
                    /* silently ignore close failures; nothing could be done anyway */
                }
            }

            return response;
        }

        /** Connect to searchd and exchange versions (internal method). */
        //private TcpClient Connect()
        //{
        //    TcpClient sock;
        //    try
        //    {
        //        //sock = new Socket(_host, _port);
        //        //sock = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IPv4);
        //        sock = new TcpClient(_host, _port);
        //        //sock.ReceiveTimeout = SPH_CLIENT_TIMEOUT_MILLISEC;               
        //    }
        //    catch (Exception e)
        //    {
        //        _error = "connection to " + _host + ":" + _port + " failed: " + e.Message;
        //        return null;
        //    }

        //    NetworkStream ns = null;

        //    try
        //    {
        //        ns = sock.GetStream();

        //        BinaryReader sr = new BinaryReader(ns);
        //        BinaryWriter sw = new BinaryWriter(ns);

        //        WriteToStream(sw, 1);
        //        sw.Flush();
        //        int version = 0;
        //        version = ReadInt32(sr);

        //        if (version < 1)
        //        {
        //            sock.Close();
        //            _error = "expected searchd protocol version 1+, got version " + version;
        //            return null;
        //        }

        //        //WriteToStream(sw, VER_MAJOR_PROTO);
        //        //sw.Flush();

        //        WriteToStream(sw, (short)4); // COMMAND_Persist
        //        WriteToStream(sw, (short)0); //PERSIST_COMMAND_VERSION
        //        WriteToStream(sw, 4); // COMMAND_LENGTH
        //        WriteToStream(sw, 1); // PERSIST_COMMAND_BODY
        //        sw.Flush();
        //    }
        //    catch (IOException e)
        //    {
        //        _error = "Connect: Read from socket failed: " + e.Message;
        //        try
        //        {
        //            sock.Close();
        //        }
        //        catch (IOException e1)
        //        {
        //            _error = _error + " Cannot close socket: " + e1.Message;
        //        }
        //        return null;
        //    }
        //    return sock;
        //}

        #endregion

        #region Network IO Helpers
        private string ReadUtf8(BinaryReader br)
        {
            int length = ReadInt32(br);

            if (length > 0)
            {
                byte[] data = br.ReadBytes(length);
                return Encoding.UTF8.GetString(data);
            }
            return "";
        }

        private short ReadInt16(BinaryReader br)
        {
            byte[] idata = br.ReadBytes(2);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(idata);
            return BitConverter.ToInt16(idata, 0);
            //return BitConverter.ToInt16(_Reverse(idata), 0);
        }
        private int ReadInt32(BinaryReader br)
        {
            byte[] idata = br.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(idata);
            return BitConverter.ToInt32(idata, 0);
            //return BitConverter.ToInt32(_Reverse(idata), 0);
        }

        private float ReadFloat(BinaryReader br)
        {
            byte[] idata = br.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(idata);
            return BitConverter.ToSingle(idata, 0);
            //return BitConverter.ToSingle(_Reverse(idata), 0);
        }

        private uint ReadUInt32(BinaryReader br)
        {
            byte[] idata = br.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(idata);
            return BitConverter.ToUInt32(idata, 0);
            //return BitConverter.ToUInt32(_Reverse(idata), 0);
        }
        private Int64 ReadInt64(BinaryReader br)
        {
            byte[] idata = br.ReadBytes(8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(idata);
            return BitConverter.ToInt64(idata, 0);
            //return BitConverter.ToInt64(_Reverse(idata), 0);
        }

        private void WriteToStream(BinaryWriter bw, short data)
        {
            byte[] d = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(d);
            bw.Write(d);
            //sw.Write(_Reverse(d));
        }
        private void WriteToStream(BinaryWriter bw, int data)
        {
            byte[] d = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(d);
            bw.Write(d);
            //sw.Write(_Reverse(d));
        }
        private void WriteToStream(BinaryWriter bw, float data)
        {
            byte[] d = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(d);
            bw.Write(d);
            //sw.Write(_Reverse(d));
        }

        private void WriteToStream(BinaryWriter bw, long data)
        {
            byte[] d = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(d);
            bw.Write(d);
            //sw.Write(_Reverse(d));
        }
        private void WriteToStream(BinaryWriter bw, byte[] data)
        {
            bw.Write(data);
        }
        private void WriteToStream(BinaryWriter bw, string data)
        {
            byte[] d = Encoding.UTF8.GetBytes(data);
            WriteToStream(bw, d.Length);
            bw.Write(d);
        }
        #endregion

        #region Other Helpers
        static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static int ConvertToUnixTimestamp(DateTime dateTime)
        {
            TimeSpan diff = dateTime.ToUniversalTime() - _epoch;
            return Convert.ToInt32(Math.Floor(diff.TotalSeconds));
        }
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (this._conn != null)
            {
                try
                {
                    if (this._conn.Connected)
                        this._conn.Close();
                }
                finally
                {
                    this._conn = null;
                }
            }
        }

        #endregion
    }

    public class SphinxResult
    {
        /** Full-text field namess. */
        public String[] fields;

        /** Attribute names. */
        public String[] attrNames;

        /** Attribute types (refer to SPH_ATTR_xxx constants in SphinxClient). */
        public int[] attrTypes;

        /** Retrieved matches. */
        public SphinxMatch[] matches;

        /** Total matches in this result set. */
        public int total;

        /** Total matches found in the index(es). */
        public int totalFound;

        /** Elapsed time (as reported by searchd), in seconds. */
        public float time;

        /** Per-word statistics. */
        public SphinxWordInfo[] words;

        /** Warning message, if any. */
        public String warning = null;

        /** Error message, if any. */
        public String error = null;

        /** Query status (refer to SEARCHD_xxx constants in SphinxClient). */
        private int status = -1;

        /** Trivial constructor, initializes an empty result set. */
        public SphinxResult()
        {
            this.attrNames = new String[0];
            this.matches = new SphinxMatch[0]; ;
            this.words = new SphinxWordInfo[0];
            this.fields = new String[0];
            this.attrTypes = new int[0];
        }

        public bool Success
        {
            get
            {
                return this.status == SphinxClient.SEARCHD_OK;
            }
        }

        /** Get query status. */
        public int getStatus()
        {
            return status;
        }

        /** Set query status (accessible from API package only). */
        internal void setStatus(int status)
        {
            this.status = status;
        }
    }

    public class SphinxMatch
    {
        /** Matched document ID. */
        public long docId;

        /** Matched document weight. */
        public int weight;

        /** Matched document attribute values. */
        public ArrayList attrValues;

        /** Trivial constructor. */
        public SphinxMatch(long docId, int weight)
        {
            this.docId = docId;
            this.weight = weight;
            this.attrValues = new ArrayList();
        }
    }

    public class SphinxWordInfo
    {
        /** Word form as returned from search daemon, stemmed or otherwise postprocessed. */
        public String word;

        /** Total amount of matching documents in collection. */
        public long docs;

        /** Total amount of hits (occurences) in collection. */
        public long hits;

        /** Trivial constructor. */
        public SphinxWordInfo(String word, long docs, long hits)
        {
            this.word = word;
            this.docs = docs;
            this.hits = hits;
        }
    }
}