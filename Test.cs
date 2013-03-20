
using System;
using System.Collections.Generic;

public class Test {
	public static void Main() {
		string s;

		{
			var jx = JxFmt.Acquire();

			jx.BeginObject()
				.Name("foo").Value(10.012436f)
				.Name("bar").BeginObject()
					.Name("baz").BeginArray()
						.BeginObject()
							.Name("blarg").Null()
						.EndObject()
						.Value(true)
						.Value(false)
						.Value("quux")
					.EndArray()
				.EndObject()
			.EndObject();

			s = jx.ToString();
			JxFmt.Release(jx);
		}

		Console.WriteLine(s);

		{
			var jx = new JxIter(s);

			if (jx.Next() == JxTok.Object)
				DumpObject(ref jx);
		}
	}

	static void DumpObject(ref JxIter jx) {
		Console.WriteLine("--\tBEGIN OBJECT", jx.depth);

		for (JxTok t; (t = jx.Next()) > JxTok.End;) {
			Console.WriteLine("{0}\tNAME {1}", jx.depth, jx.ToString(t));

			t = jx.Next();

			if (t == JxTok.Object)
				DumpObject(ref jx);
			else if (t == JxTok.Array)
				DumpArray(ref jx);
			else
				Console.WriteLine("{0}\tVALUE {1} ({2})", jx.depth, jx.ToString(t), t);
		}

		Console.WriteLine("--\tEND OBJECT", jx.depth);
	}

	static void DumpArray(ref JxIter jx) {
		Console.WriteLine("--\tBEGIN ARRAY", jx.depth);

		for (JxTok t; (t = jx.Next()) > JxTok.End;) {
			if (t == JxTok.Object)
				DumpObject(ref jx);
			else if (t == JxTok.Array)
				DumpArray(ref jx);
			else
				Console.WriteLine("{0}\tVALUE {1} ({2})", jx.depth, jx.ToString(t), t);
		}

		Console.WriteLine("--\tEND ARRAY", jx.depth);
	}
}

