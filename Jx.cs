
//////////////////////////////////////////////////////////////////////////
//
// Source: http://github.com/afterwise/jx
// Contact: afterwise -> gmail.com
//
// This is free and unencumbered software released into the public domain.
// 
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
// 
// In jurisdictions that recognize copyright laws, the author or authors
// of this software dedicate any and all copyright interest in the
// software to the public domain. We make this dedication for the benefit
// of the public at large and to the detriment of our heirs and
// successors. We intend this dedication to be an overt act of
// relinquishment in perpetuity of all present and future rights to this
// software under copyright law.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
// 
// For more information, please refer to <http://unlicense.org/>
//
//////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public enum JxTok {
	EOF,
	End,

	Object,
	Array,
	Group,

	Setter,
	Sym,
	Str,

	Int,
	Float
}

public enum JxAccess {
	Random,
	Linear
}

public struct JxProp {
	public string key;
	public object val;
}

public unsafe struct JxIter {
	public const int smax = 1024;

	public int rpos;
	public int wpos;
	public int depth;
	public string json;

	public fixed char strval[smax];
	public long intval;
	public float floatval;

	public JxIter(string xjson) {
		rpos = 0;
		wpos = 0;
		depth = 0;
		json = xjson;

		intval = 0;
		floatval = 0f;
	}

	public bool StrEq(string s) {
		int n = s != null ? s.Length : 0;

		if (n != wpos)
			return false;

		fixed (char *rs = strval) {
			for (int i = 0; i < n; ++i)
				if (rs[i] != s[i])
					return false;
		}

		return true;
	}

	JxTok EatSym(char *s) {
		char c = s[rpos];

		fixed (char *rs = strval) {
			for (;;) {
				if (wpos == smax)
					return JxTok.EOF;

				switch (c) {
				case ' ':
				case '\t':
				case '\f':
				case '\r':
				case '\n':
				case ',':
				case ':':
				case '[':
				case '{':
				case '(':
				case ']':
				case '}':
				case ')':
				case '"':
					rs[wpos] = '\0';
					return JxTok.Sym;

				default:
					rs[wpos++] = c;
					c = s[++rpos];
					break;
				}
			}
		}
	}

	JxTok EatStr(char *s) {
		rpos++;

		fixed (char *rs = strval) {
			for (;;) {
				if (wpos == smax)
					return JxTok.EOF;

				switch (s[rpos]) {
				case '"':
					rpos++;
					rs[wpos] = '\0';
					return JxTok.Str;

				case '\\':
					switch (s[++rpos]) {
					case '"':
						rpos++;
						rs[wpos++] = '\"';
						break;

					case '\\':
						rpos++;
						rs[wpos++] = '\\';
						break;

					case 'b':
						rpos++;
						rs[wpos++] = '\b';
						break;

					case 'f':
						rpos++;
						rs[wpos++] = '\f';
						break;

					case 'r':
						rpos++;
						rs[wpos++] = '\r';
						break;

					case 'n':
						rpos++;
						rs[wpos++] = '\n';
						break;

					case 't':
						rpos++;
						rs[wpos++] = '\t';
						break;

					case 'u':
						{
							byte *x = stackalloc byte[128];
							x['0'] = 0x0;
							x['1'] = 0x1;
							x['2'] = 0x2;
							x['3'] = 0x3;
							x['4'] = 0x4;
							x['5'] = 0x5;
							x['6'] = 0x6;
							x['7'] = 0x7;
							x['8'] = 0x8;
							x['9'] = 0x9;
							x['A'] = 0xa;
							x['B'] = 0xb;
							x['C'] = 0xc;
							x['D'] = 0xd;
							x['E'] = 0xe;
							x['F'] = 0xf;
							x['a'] = 0xa;
							x['b'] = 0xb;
							x['c'] = 0xc;
							x['d'] = 0xd;
							x['e'] = 0xe;
							x['f'] = 0xf;

							var e = Encoding.Unicode;

							byte *b = stackalloc byte[2];
							b[0] = (byte) ((x[s[rpos + 3] & 0x7f] << 4) + x[s[rpos + 4] & 0x7f]);
							b[1] = (byte) ((x[s[rpos + 1] & 0x7f] << 4) + x[s[rpos + 2] & 0x7f]);
							rpos += 5;

							e.GetChars(b, 2, rs + wpos, 1);
							wpos++;
						}
						break;

					default:
						return JxTok.EOF;
					}
					break;

				case '\0':
					return JxTok.EOF;

				default:
					rs[wpos++] = s[rpos++];
					break;
				}
			}
		}
	}

	JxTok EatNum(char *s) {
		var t = JxTok.Int;
		int sgn = 1;
		int lpos = rpos;
		char c;

		for (;;) {
			switch (c = s[rpos]) {
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				intval = (intval << 1) + (intval << 3) + (s[rpos++] ^ '0');
				break;

			case '.':
				lpos = ++rpos;
				floatval = intval;
				intval = 0;
				t = JxTok.Float;
				break;

			case '-':
				sgn = -1;
				goto case '+';

			case '+':
				if (lpos != rpos)
					return JxTok.EOF;

				rpos++;
				break;

			default:
				if (t == JxTok.Float) {
					if (intval > 0) {
						long x = 1;

						for (int i = 0; i < rpos - lpos; ++i)
							x *= 10;

						floatval += (float) intval / (float) x;
					}

					floatval *= sgn;
				} else
					intval *= sgn;

				if (c == 'e' || c == 'E') {
					rpos++;

					if (t == JxTok.Int)
						floatval = intval;

					intval = 0;

					if (EatNum(s) != JxTok.Int)
						return JxTok.EOF;

					long x = 1;

					for (int i = 0, j = (int) (intval >> 31) | 1; i != intval; i += j)
						x *= 10;

					floatval *= intval >= 0 ? (float) x : 1f / x;
					t = JxTok.Float;
				}

				return t;
			}
		}
	}

	public JxTok Next() {
		fixed(char *s = json)
			return Scan(s);
	}

	unsafe JxTok Scan(char *s) {
		wpos = 0;
		intval = 0;
		floatval = 0f;

		for (;;)
			switch (s[rpos]) {
			case '{':
				rpos++;
				depth++;
				return JxTok.Object;

			case '[':
				rpos++;
				depth++;
				return JxTok.Array;

			case '(':
				rpos++;
				depth++;
				return JxTok.Group;

			case '}':
			case ']':
			case ')':
				rpos++;
				depth--;
				return JxTok.End;

			case '"':
				if (EatStr(s) != JxTok.Str)
					return JxTok.EOF;

				if (s[rpos] == ':') {
					rpos++;
					return JxTok.Setter;
				}

				return JxTok.Str;

			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				return EatNum(s);

			case '-':
			case '+':
				if ((uint) (s[rpos + 1] - '0') <= 9)
					return EatNum(s);

				goto default;

			case ' ':
			case '\t':
			case '\f':
			case '\r':
			case '\n':
			case ',':
			case ':':
				rpos++;
				break;

			case '\0':
				return JxTok.EOF;

			default:
				if (EatSym(s) != JxTok.Sym)
					return JxTok.EOF;

				if (s[rpos] == ':') {
					rpos++;
					return JxTok.Setter;
				}

				return JxTok.Sym;
			}
	}

	public unsafe void Skip(JxTok t) {
		if (t == JxTok.Object || t == JxTok.Array) {
			int cur = depth;

			fixed (char *s = json)
				do t = Scan(s);
				while (t != JxTok.EOF && depth >= cur);
		}
	}

	public void SkipToEnd(JxTok t) {
		if (t > JxTok.End)
			while ((t = Next()) > JxTok.End)
				Skip(t);
	}

	public unsafe string ToString(JxTok t) {
		if ((uint) (t - JxTok.Object) <= JxTok.Group - JxTok.Object) {
			int p = rpos - 1;
			Skip(t);

			fixed (char *s = json)
				return new string(s, p, rpos - p);
		}

		if ((uint) (t - JxTok.Setter) <= JxTok.Str - JxTok.Setter)
			fixed (char *s = strval)
				return new string(s);

		if (t == JxTok.Int)
			return Convert.ToString(intval, CultureInfo.InvariantCulture);

		if (t == JxTok.Float)
			return Convert.ToString(floatval, CultureInfo.InvariantCulture);

		return null;
	}

	public void CopyNumber(out int v, JxTok t) {
		v = t == JxTok.Int ? (int) intval : (int) floatval;
	}

	public void CopyNumber(out long v, JxTok t) {
		v = t == JxTok.Int ? intval : (long) floatval;
	}

	public void CopyNumber(out float v, JxTok t) {
		v = t == JxTok.Int ? (float) intval : floatval;
	}

#if JX_GRANDFATHER
	object EatRandomAccessObject() {
		var o = new Dictionary<string, object>();

		for (JxTok t; (t = Next()) > JxTok.End;) {
			string n = ToString(t);
			o[n] = ToObject(Next(), JxAccess.Random);
		}

		return o;
	}

	object EatLinearAccessObject() {
		var o = new List<JxProp>();

		for (JxTok t; (t = Next()) > JxTok.End;) {
			string n = ToString(t);
			o.Add(new JxProp {key = n, val = ToObject(Next(), JxAccess.Linear)});
		}

		return o;
	}

	object EatArray(JxAccess x) {
		List<object> a = new List<object>();

		for (JxTok t; (t = Next()) > JxTok.End;)
			a.Add(ToObject(t, x));

		return a;
	}

	public unsafe object ToObject(JxTok t, JxAccess x) {
		if (t == JxTok.Object)
			return x == JxAccess.Random ? EatRandomAccessObject() : EatLinearAccessObject();

		if (t == JxTok.Array)
			return EatArray(x);

		if (t == JxTok.Str)
			fixed (char *s = strval)
				return new string(s);

		if (t == JxTok.Int)
			return intval;

		if (t == JxTok.Float)
			return floatval;

		if (t == JxTok.Sym) {
			if (StrEq("true"))
				return true;

			if (StrEq("false"))
				return false;
		}

		return null;
	}
#endif // JX_GRANDFATHER
}

public class JxFmt {
	readonly static Stack<JxFmt> _pool = new Stack<JxFmt>();

	static JxFmt() {
		_pool.Push(new JxFmt());
	}

	public static void AddToPool(int n) {
		for (int i = 0; i < n; ++i)
			_pool.Push(new JxFmt());
	}

	public static JxFmt Acquire() {
		return _pool.Pop();
	}

	public static void Release(JxFmt j) {
		j.Clear();
		_pool.Push(j);
	}

	StringBuilder _buf;
	ulong _mask; // 64 levels or bust

	private JxFmt() {
		_buf = new StringBuilder(2048);
	}

	public JxFmt Clear() {
		_buf.Length = 0;
		_mask = 0;

		return this;
	}

	void Begin(char c) {
		VanillaPrefix();
		_buf.Append(c);
		_mask <<= 1;
	}

	void End(char c) {
		_buf.Append(c);
		_mask = (_mask >> 1) | 1;
	}

	public JxFmt BeginObject() {
		Begin('{');
		return this;
	}

	public JxFmt EndObject() {
		End('}');
		return this;
	}

	public JxFmt BeginArray() {
		Begin('[');
		return this;
	}

	public JxFmt EndArray() {
		End(']');
		return this;
	}

	void StringPrefix() {
		if ((_mask & 1) != 0)
			_buf.Append(",\"");
		else
			_buf.Append('"');
	}

	void VanillaPrefix() {
		if ((_mask & 1) != 0)
			_buf.Append(",");
	}

	public JxFmt Name(string name) {
		StringPrefix();
		_buf.Append(name);
		_buf.Append("\":");
		_mask &= ~((ulong) 1);

		return this;
	}

	public JxFmt Value(bool value) {
		VanillaPrefix();
		_buf.Append(value ? "true" : "false");
		_mask |= 1;

		return this;
	}

	public JxFmt Value(long value) {
		VanillaPrefix();
		_buf.Append(value);
		_mask |= 1;

		return this;
	}

	public JxFmt Value(float value) {
		VanillaPrefix();
		_buf.AppendFormat(CultureInfo.InvariantCulture, "{0:R}", value);
		_mask |= 1;

		return this;
	}

	public JxFmt Value(string value) {
		StringPrefix();
		_buf.Append(value);
		_buf.Append('"');
		_mask |= 1;

		return this;
	}

	public JxFmt Value(JxFmt j) {
		VanillaPrefix();
		_buf.Append(j.ToString());
		_mask |= 1;

		return this;
	}

#if JX_GRANDFATHER
	public JxFmt Value(Dictionary<string, object> o) {
		BeginObject();

		foreach (var p in o)
			Name(p.Key).Value(p.Value);

		EndObject();
		_mask |= 1;

		return this;
	}

	public JxFmt Value(List<JxProp> o) {
		BeginObject();

		foreach (var p in o)
			Name(p.key).Value(p.val);

		EndObject();
		_mask |= 1;

		return this;
	}

	public JxFmt Value(List<object> l) {
		BeginArray();

		foreach (var v in l)
			Value(v);

		EndArray();
		_mask |= 1;

		return this;
	}
#endif

	public JxFmt Value(object o) {
		if (o is float || o is double)
			Value(Convert.ToSingle(o));
		else if (o is int || o is long)
			Value(Convert.ToInt64(o));
		else if (o is bool)
			Value((bool) o);
		else if (o is string)
			Value((string) o);
#if JX_GRANDFATHER
		else if (o is List<object>)
			Value((List<object>) o);
		else if (o is List<JxProp>)
			Value((List<JxProp>) o);
		else if (o is Dictionary<string, object>)
			Value((Dictionary<string, object>) o);
#endif
		else
			Null();

		return this;
	}

	public JxFmt Null() {
		VanillaPrefix();
		_buf.Append("null");
		_mask |= 1;

		return this;
	}

	public override string ToString() {
		return _buf.ToString();
	}
}

