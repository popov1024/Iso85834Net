﻿using System;
using System.IO;
using System.Text;
using C5;
using Iso85834Net.Parse;
using Iso85834Net.Util;

namespace Iso85834Net.Codecs
{
    /// <summary>
    ///     A codec to manage subfields inside a field of a certain type.
    /// </summary>
    public class CompositeField : ICustomBinaryField
    {
        private ArrayList<FieldParseInfo> parsers;
        public ArrayList<IsoValue> Values { get; set; }

        public object DecodeField(string value)
        {
            var vals = new ArrayList<IsoValue>(parsers.Count);
            var buf = value.GetSbytes();
            var pos = 0;
            try
            {
                foreach (var fpi in parsers)
                {
                    var v = fpi.Parse(0,
                        buf,
                        pos,
                        fpi.Decoder);
                    if (v == null) continue;
                    pos += fpi.Encoding.GetBytes(v.ToString()).Length;
                    switch (v.Type)
                    {
                        case IsoType.LLVAR:
                        case IsoType.LLBIN:
                            pos += 2;
                            break;
                        case IsoType.LLLVAR:
                        case IsoType.LLLBIN:
                            pos += 3;
                            break;
                        case IsoType.LLLLBIN:
                        case IsoType.LLLLVAR:
                            pos += 4;
                            break;
                    }
                    vals.Add(v);
                }
                var f = new CompositeField
                {
                    Values = vals
                };
                return f;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string EncodeField(object val)
        {
            try
            {
                var value = (CompositeField) val;
                Encoding encoding = null;
                var bout = new MemoryStream();
                foreach (var v in value.Values)
                {
                    v.Write(bout,
                        false,
                        true);
                    if (encoding == null) encoding = v.Encoding;
                }
                var buf = bout.ToArray();
                if (encoding == null) return Encoding.UTF8.GetString(buf);
                return encoding.GetString(buf);
            }
            catch (IOException)
            {
                return string.Empty;
            }
        }

        public object DecodeBinaryField(sbyte[] buf,
            int offset,
            int length)
        {
            var vals = new ArrayList<IsoValue>(parsers.Count);
            var pos = offset;
            try
            {
                foreach (var fpi in parsers)
                {
                    var v = fpi.ParseBinary(0,
                        buf,
                        pos,
                        fpi.Decoder);
                    if (v == null) continue;
                    if (v.Type == IsoType.NUMERIC || v.Type == IsoType.DATE10 || v.Type == IsoType.DATE4 ||
                        v.Type == IsoType.DATE_EXP || v.Type == IsoType.AMOUNT || v.Type == IsoType.TIME ||
                        v.Type == IsoType.DATE12 || v.Type == IsoType.DATE14) pos += v.Length / 2 + v.Length % 2;
                    else pos += v.Length;
                    switch (v.Type)
                    {
                        case IsoType.LLVAR:
                        case IsoType.LLBIN:
                            pos++;
                            break;
                        case IsoType.LLLVAR:
                        case IsoType.LLLBIN:
                        case IsoType.LLLLVAR:
                        case IsoType.LLLLBIN:
                            pos += 2;
                            break;
                    }
                    vals.Add(v);
                }
                var f = new CompositeField
                {
                    Values = vals
                };
                return f;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public sbyte[] EncodeBinaryField(object val)
        {
            var stream = new MemoryStream();
            try
            {
                var value = (CompositeField) val;
                foreach (var v in value.Values)
                    v.Write(stream,
                        true,
                        true);
            }
            catch (IOException)
            {
                //shouldn't happen
            }
            return stream.ToArray().ToSbytes();
        }

        public CompositeField AddValue(IsoValue value)
        {
            if (Values == null) Values = new ArrayList<IsoValue>(4);
            Values.Add(value);
            return this;
        }

        public CompositeField AddValue(object val,
            ICustomField encoder,
            IsoType t,
            int length)
        {
            return AddValue(t.NeedsLength()
                ? new IsoValue(t,
                    val,
                    length,
                    encoder)
                : new IsoValue(t,
                    val,
                    encoder));
        }

        public IsoValue GetField(int idx)
        {
            if (idx < 0 || idx >= Values.Count) return null;
            return Values[idx];
        }

        public object GetObjectValue(int idx)
        {
            var v = GetField(idx);
            return v.Value;
        }

        public CompositeField AddParser(FieldParseInfo fpi)
        {
            if (parsers == null) parsers = new ArrayList<FieldParseInfo>(4);
            parsers.Add(fpi);
            return this;
        }

        public ArrayList<FieldParseInfo> GetParsers()
        {
            return parsers;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("CompositeField[");
            if (Values != null)
            {
                var first = true;
                foreach (var v in Values)
                {
                    if (first) first = false;
                    else sb.Append(',');
                    sb.Append(v.Type);
                }
            }
            return sb.Append(']').ToString();
        }
    }
}