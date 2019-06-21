namespace SphinxClassLibrary
{
    using System;


    public class Attribute
    {
        public string Name { get; set; }
        public AttributeTypes Type { get; set; }

        public Attribute(string _name, AttributeTypes _type)
        {
            Name = _name;
            Type = _type;
        }

        public override string ToString()
        {
            return string.Format("Attribute={0} Type={1}", Name, Type);
        }

        public bool isMVA
        {
            get
            {
                return Type == AttributeTypes.SPH_ATTR_MULTI;
            }
        }
    }

    public class AttributeValue
    {
        public Attribute Attribute { get; private set; }
        public object RawValue { get; private set; }

        public AttributeValue(Attribute _attribute)
        {
            Attribute = _attribute;
        }

        public override string ToString()
        {
            return string.Format(Attribute + " Value={0}", RawValue);
        }

        private void AssertType(AttributeTypes _check)
        {
            if (Attribute.Type != _check)
                throw new SphinxClientException(string.Format("Incorrect attribute type, expected {0} got {1}", Attribute.Type, _check));
        }

        public void SetRawValue(object _v, AttributeTypes _t)
        {
            AssertType(_t);
            RawValue = _v;
        }

        internal void ReadFrom(IO.SphinxBinaryReader _br)
        {
            switch (Attribute.Type)
            {
                case AttributeTypes.SPH_ATTR_MULTI:
                    int[] values = new int[_br.ReadInt()];
                    for (int i = 0; i < values.Length; i++)
                        values[i] = _br.ReadInt();
                    RawValue = values;
                    break;
                case AttributeTypes.SPH_ATTR_BIGINT:
                    RawValue = _br.ReadLong();
                    break;
                case AttributeTypes.SPH_ATTR_FLOAT:
                    RawValue = _br.ReadFloat();
                    break;
                case AttributeTypes.SPH_ATTR_STRING:
                    RawValue = _br.ReadStr();
                    break;
                case AttributeTypes.SPH_ATTR_BOOL:
                    RawValue = _br.ReadInt() != 0;
                    break;
                default:
                    RawValue = _br.ReadInt();
                    break;
             }
        }

        internal void WriteTo(IO.SphinxBinaryWriter _wr)
        {
            switch (Attribute.Type)
            {
                case AttributeTypes.SPH_ATTR_MULTI:
                    int[] values = (int[])RawValue;
                    _wr.WriteInt(values.Length);
                    for (int i = 0; i < values.Length; i++)
                        _wr.WriteInt(values[i]);
                    break;
                case AttributeTypes.SPH_ATTR_BIGINT:
                    _wr.WriteLong((long)RawValue);
                    break;
                case AttributeTypes.SPH_ATTR_FLOAT:
                    _wr.WriteFloat((float)RawValue);
                    break;
                case AttributeTypes.SPH_ATTR_STRING:
                    _wr.WriteStr((string)RawValue);
                    break;
                case AttributeTypes.SPH_ATTR_BOOL:
                    _wr.WriteInt((bool)RawValue ? 1 : 0);
                    break;
                default:
                    _wr.WriteInt((int)RawValue);
                    break;
            }
        }

        public Int32 AsTimestamp
        {
            get
            {
                AssertType(AttributeTypes.SPH_ATTR_TIMESTAMP);
                return (Int32)RawValue;
            }
            set { SetRawValue(value, AttributeTypes.SPH_ATTR_TIMESTAMP); }
        }

        public bool AsBool
        {
            get
            {
                AssertType(AttributeTypes.SPH_ATTR_BOOL);
                return (bool)RawValue;
            }
            set { SetRawValue(value, AttributeTypes.SPH_ATTR_BOOL); }
        }

        public Int32 AsInt
        {
            get
            {
                AssertType(AttributeTypes.SPH_ATTR_INTEGER);
                return (Int32)RawValue;
            }
            set { SetRawValue(value, AttributeTypes.SPH_ATTR_INTEGER); }
        }

        public UInt32 AsOrdinal
        {
            get
            {
                AssertType(AttributeTypes.SPH_ATTR_ORDINAL);
                return (UInt32)RawValue;
            }
            set { SetRawValue(value, AttributeTypes.SPH_ATTR_ORDINAL); }
        }

        public Int64 AsLong
        {
            get
            {
                AssertType(AttributeTypes.SPH_ATTR_BIGINT);
                return (Int64)RawValue;
            }
            set { SetRawValue(value, AttributeTypes.SPH_ATTR_BIGINT); }
        }

        public float AsFloat
        {
            get
            {
                AssertType(AttributeTypes.SPH_ATTR_FLOAT);
                return (float)RawValue;
            }
            set { SetRawValue(value, AttributeTypes.SPH_ATTR_FLOAT); }
        }

        public string AsString
        {
            get
            {
                AssertType(AttributeTypes.SPH_ATTR_STRING);
                return (string)RawValue;
            }
            set { SetRawValue(value, AttributeTypes.SPH_ATTR_STRING); }
        }

        public int[] AsMVA
        {
            get
            {
                AssertType(AttributeTypes.SPH_ATTR_MULTI);
                return (int[])RawValue;
            }
            set { SetRawValue(value, AttributeTypes.SPH_ATTR_MULTI); }
        }
    }
}
