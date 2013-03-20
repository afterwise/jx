
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

using System.Collections.Generic;
using System.Text;

public enum JxTok {
	EOF,
	End,
	Object,
	Array,
	Str,
	Int,
	Float,
	Bool,
	Null
}

public unsafe struct JxIter {

	public int rpos;
	public int wpos;
	public int depth;
	public string json;

	public fixed char strval[1024];
	public long intval;
	public float floatval;
	public bool boolval;

	public JxIter(string xjson) {
		rpos = 0;
		wpos = 0;
		depth = 0;
		json = xjson;

		intval = 0;
		floatval = 0f;
		boolval = false;
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

	bool EatSym(string t) {
		int n = t.Length;

		fixed (char *s = json) {
			for (int i = 0; i < n; ++i)
				if (s[i + rpos] != t[i])
					return false;

			if (s[rpos + n] >= 'a' && s[rpos + n] <= 'z')
				return false;
		}

		rpos += n;
		return true;
	}

	JxTok EatStr(char *s) {
		rpos++;

		fixed (char *rs = strval) {
			for (;;) {
				if (wpos == 1024)
					return JxTok.EOF;

				switch (s[rpos]) {
				case '"':
					rpos++;
					rs[wpos++] = '\0';
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

		for (;;) {
			switch (s[rpos]) {
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
				if (wpos != 0)
					return JxTok.EOF;

				rpos++;
				break;

			default:
				if (t == JxTok.Float && intval > 0) {
					long x = 1;

					for (int i = 0; i < rpos - lpos; ++i)
						x *= 10;

					floatval += (float) intval / (float) x;
					floatval *= sgn;
				} else
					intval *= sgn;

				return t;
			}
		}
	}

	public JxTok Next() {
		wpos = 0;
		intval = 0;
		floatval = 0f;
		boolval = false;

		fixed(char *s = json) {
			for (;;) {
				switch (s[rpos]) {
				case '{':
					rpos++;
					depth++;
					return JxTok.Object;

				case '[':
					rpos++;
					depth++;
					return JxTok.Array;

				case '}':
				case ']':
					rpos++;
					depth--;
					return JxTok.End;

				case '"':
					return EatStr(s);

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
				case '-':
				case '+':
					return EatNum(s);

				case 't':
					if (EatSym("true")) {
						boolval = true;
						return JxTok.Bool;
					}

					return JxTok.EOF;

				case 'f':
					if (EatSym("false"))
						return JxTok.Bool;

					return JxTok.EOF;

				case 'n':
					if (EatSym("null"))
						return JxTok.Null;

					return JxTok.EOF;

				case ' ':
				case '\t':
				case '\f':
				case '\r':
				case '\n':
				case ',':
				case ':':
					rpos++;
					break;

				default:
					return JxTok.EOF;
				}
			}
		}
	}

	public void Skip(JxTok t) {
		if (t == JxTok.Object || t == JxTok.Array) {
			int d = depth;

			do t = Next();
			while ((t > JxTok.End || depth > d) && t != JxTok.EOF);
		}
	}

	public unsafe string ToString(JxTok t) {
		if (t == JxTok.Object || t == JxTok.Array) {
			Skip(t);
			return null; // not supported
		}

		if (t == JxTok.Str)
			fixed (char *s = strval) {
				return new string(s);
			}

		if (t == JxTok.Int)
			return System.Convert.ToString(intval);

		if (t == JxTok.Float)
			return System.Convert.ToString(floatval);

		if (t == JxTok.Bool)
			return System.Convert.ToString(boolval);

		return null;
	}

	Dictionary<string, object> EatObject() {
		var o = new Dictionary<string, object>();

		for (JxTok t; (t = Next()) != JxTok.EOF;) {
			if (t == JxTok.End)
				break;

			if (t != JxTok.Str)
				continue;

			string n = ToString(t);
			o[n] = ToObject(Next());
		}

		return o;
	}

	List<object> EatArray() {
		List<object> a = new List<object>();

		for (JxTok t; (t = Next()) != JxTok.EOF;) {
			if (t == JxTok.End)
				break;

			a.Add(ToObject(t));
		}

		return a;
	}

	public unsafe object ToObject(JxTok t) {
		if (t == JxTok.Object)
			return EatObject();

		if (t == JxTok.Array)
			return EatArray();

		if (t == JxTok.Str)
			fixed (char *s = strval) {
				return new string(s);
			}

		if (t == JxTok.Int)
			return intval;

		if (t == JxTok.Float)
			return floatval;

		if (t == JxTok.Bool)
			return boolval;

		return null;
	}
}

public class JxFmt {
	readonly static Stack<JxFmt> _pool = new Stack<JxFmt>();

	static JxFmt() {
		_pool.Push(new JxFmt());
		_pool.Push(new JxFmt());
		_pool.Push(new JxFmt());
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
		if ((_mask & 1) != 0)
			_buf.Append(',');

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

	void LiteralPrefix() {
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
		LiteralPrefix();
		_buf.Append(value ? "true" : "false");
		_mask |= 1;

		return this;
	}

	public JxFmt Value(long value) {
		LiteralPrefix();
		_buf.Append(value);
		_mask |= 1;

		return this;
	}

	public JxFmt Value(float value) {
		LiteralPrefix();
		_buf.Append(value);
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
		LiteralPrefix();
		_buf.Append(j.ToString());
		_mask |= 1;

		return this;
	}

	public JxFmt Value(Dictionary<string, object> d) {
		LiteralPrefix();
		BeginObject();

		foreach (var kv in d)
			Name(kv.Key).Value(kv.Value);

		EndObject();
		_mask |= 1;

		return this;
	}

	public JxFmt Value(List<object> l) {
		LiteralPrefix();
		BeginArray();

		foreach (var v in l)
			Value(v);

		EndArray();
		_mask |= 1;

		return this;
	}

	public JxFmt Value(object o) {
		if (o is float || o is double)
			Value(System.Convert.ToSingle(o));
		else if (o is int || o is long)
			Value(System.Convert.ToInt64(o));
		else if (o is string)
			Value((string) o);
		else if (o is Dictionary<string, object>)
			Value((Dictionary<string, object>) o);
		else if (o is List<object>)
			Value((List<object>) o);
		else
			Null();

		return this;
	}

	public JxFmt Null() {
		LiteralPrefix();
		_buf.Append("null");
		_mask |= 1;

		return this;
	}

	public override string ToString() {
		return _buf.ToString();
	}
}
