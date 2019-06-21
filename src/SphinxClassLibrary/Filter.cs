namespace SphinxClassLibrary
{
    abstract class Filter
    {
        public string Attribute { get; set; }
        public FilterTypes Type { get; protected set; }
        public bool Exclude { get; set; }

        public Filter(string _attr, FilterTypes _type, bool _exclude)
        {
            Attribute = _attr;
            Type = _type;
            Exclude = _exclude;
        }

        protected abstract void InternalWriteTo(IO.SphinxBinaryWriter _bw);

        internal void WriteTo(IO.SphinxBinaryWriter _bw)
        {
            _bw.WriteStr(Attribute);
            _bw.WriteInt((int)Type);
            InternalWriteTo(_bw);
            _bw.WriteInt((Exclude ? 1 : 0));
        }
    }

    class FilterValues : Filter
    {
        public long[] Values { get; set; }

        public FilterValues(string _attr, bool _exclude, long[] _values)
            : base(_attr, FilterTypes.SPH_FILTER_VALUES, _exclude)
        {
            Values = _values;
        }

        protected override void InternalWriteTo(IO.SphinxBinaryWriter _bw)
        {
            _bw.WriteInt(Values.Length);
            foreach (long v in Values)
                _bw.WriteLong(v);
        }
    }

    class FilterRange : Filter
    {
        public long Min { get; set; }
        public long Max { get; set; }

        public FilterRange(string _attr, bool _exclude, long _min, long _max)
            : base(_attr, FilterTypes.SPH_FILTER_RANGE, _exclude)
        {
            Min = _min;
            Max = _max;
        }

        protected override void InternalWriteTo(IO.SphinxBinaryWriter _bw)
        {
            _bw.WriteLong(Min);
            _bw.WriteLong(Max);
        }
    }

    class FilterRangeFloat : Filter
    {
        public float Min { get; set; }
        public float Max { get; set; }

        public FilterRangeFloat(string _attr, bool _exclude, float _min, float _max)
            : base(_attr, FilterTypes.SPH_FILTER_FLOATRANGE, _exclude)
        {
            Min = _min;
            Max = _max;
        }

        protected override void InternalWriteTo(IO.SphinxBinaryWriter _bw)
        {
            _bw.WriteFloat(Min);
            _bw.WriteFloat(Max);
        }
    }
}
