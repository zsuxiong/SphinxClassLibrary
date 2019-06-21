namespace SphinxClassLibrary
{
    using System;
    using System.Runtime.InteropServices;


    /// known searchd status codes
    public enum SeachdStatusCodes : uint
    {
        SEARCHD_OK = 0,
        SEARCHD_ERROR = 1,
        SEARCHD_RETRY = 2,
        SEARCHD_WARNING = 3
    }

    /// known match modes
    public enum MatchModes : uint
    {
        SPH_MATCH_ALL = 0,
        SPH_MATCH_ANY = 1,
        SPH_MATCH_PHRASE = 2,
        SPH_MATCH_BOOLEAN = 3,
        SPH_MATCH_EXTENDED = 4,
        SPH_MATCH_FULLSCAN = 5,
        SPH_MATCH_EXTENDED2 = 6
    }

    /// known ranking modes (ext2 only)
    public enum RankingModes : uint
    {
        SPH_RANK_PROXIMITY_BM25 = 0,
        SPH_RANK_BM25 = 1,
        SPH_RANK_NONE = 2,
        SPH_RANK_WORDCOUNT = 3,
        SPH_RANK_PROXIMITY = 4,
        SPH_RANK_MATCHANY = 5,
        SPH_RANK_FIELDMASK = 6,
        SPH_RANK_SPH04 = 7,
        SPH_RANK_TOTAL = 8
    }

    /// known sort modes
    public enum SortModes : uint
    {
        SPH_SORT_RELEVANCE = 0,
        SPH_SORT_ATTR_DESC = 1,
        SPH_SORT_ATTR_ASC = 2,
        SPH_SORT_TIME_SEGMENTS = 3,
        SPH_SORT_EXTENDED = 4,
        SPH_SORT_EXPR = 5
    }

    /// known filter types
    public enum FilterTypes : uint
    {
        SPH_FILTER_VALUES = 0,
        SPH_FILTER_RANGE = 1,
        SPH_FILTER_FLOATRANGE = 2
    }

    /// known attribute types
    public enum AttributeTypes : ulong
    {
        SPH_ATTR_INTEGER = 1,
        SPH_ATTR_TIMESTAMP = 2,
        SPH_ATTR_ORDINAL = 3,
        SPH_ATTR_BOOL = 4,
        SPH_ATTR_FLOAT = 5,
        SPH_ATTR_BIGINT = 6,
        SPH_ATTR_STRING = 7,
        SPH_ATTR_MULTI = 0x40000000UL
    }

    /// known grouping functions
    public enum GroupingFunctions : uint
    {
        SPH_GROUPBY_DAY = 0,
        SPH_GROUPBY_WEEK = 1,
        SPH_GROUPBY_MONTH = 2,
        SPH_GROUPBY_YEAR = 3,
        SPH_GROUPBY_ATTR = 4,
        SPH_GROUPBY_ATTRPAIR = 5
    }

    [Flags]
    public enum ExcerptFlags : uint
    {
        REMOVE_SPACES = 1,
        EXACT_PHRASE = 2,
        SINGLE_PASSAGE = 4,
        USE_BOUNDARIES = 8,
        WEIGHT_ORDER = 16,
        QUERY_MODE = 32,
        FORCE_ALL_WORDS = 64,
        LOAD_FILES = 128,
        ALLOW_EMPTY = 256,
        EMIT_ZONES = 512
    }

    public enum SearchdCommand : short
    {
        SEARCHD_COMMAND_SEARCH = 0,
        SEARCHD_COMMAND_EXCERPT = 1,
        SEARCHD_COMMAND_UPDATE = 2,
        SEARCHD_COMMAND_KEYWORDS = 3,
        SEARCHD_COMMAND_PERSIST = 4,
        SEARCHD_COMMAND_STATUS = 5,
        SEARCHD_COMMAND_QUERY = 6,
        SEARCHD_COMMAND_FLUSHATTRS = 7,
    };

    public enum VerCommand : short
    {
        VER_COMMAND_SEARCH = 0x118,
        VER_COMMAND_EXCERPT = 0x103,
        VER_COMMAND_UPDATE = 0x102,
        VER_COMMAND_KEYWORDS = 0x100,
        VER_COMMAND_STATUS = 0x100,
        VER_COMMAND_QUERY = 0x100,
        VER_COMMAND_FLUSHATTRS = 0x100
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct IntFloatUnion
    {
        [FieldOffset(0)]
        public int i;
        [FieldOffset(0)]
        public float f;
    }
}
