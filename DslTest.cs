
using System;
using System.Collections.Generic;

public class DslTest {
	unsafe public static void Main() {
		string s = "1 + (2 * 10) / 7 - -5";
		Console.WriteLine(s);

		var jx = new JxIter(s);
		var sp = new Stack<long>();
		var op = new Stack<char>();

		sp.Push(0);
		op.Push('+');

		for (JxTok t; (t = jx.Next()) > JxTok.EOF;) {
			long val = 0;
			bool reduce = false;

			if (t == JxTok.Group) {
				sp.Push(0);
				op.Push('+');
			} else if (t == JxTok.End) {
				val = sp.Pop();
				reduce = true;
			} else if (t == JxTok.Sym) {
				op.Push(jx.strval[0]);
			} else if (t == JxTok.Int) {
				val = jx.intval;
				reduce = true;
			}

			if (reduce) {
				switch (op.Pop()) {
				case '+':
					Console.WriteLine("-> {0} + {1} => {2}", sp.Peek(), val, sp.Peek() + val);
					val = sp.Pop() + val;
					break;

				case '-':
					Console.WriteLine("-> {0} - {1} => {2}", sp.Peek(), val, sp.Peek() - val);
					val = sp.Pop() - val;
					break;

				case '*':
					Console.WriteLine("-> {0} * {1} => {2}", sp.Peek(), val, sp.Peek() * val);
					val = sp.Pop() * val;
					break;

				case '/':
					Console.WriteLine("-> {0} / {1} => {2}", sp.Peek(), val, sp.Peek() / val);
					val = sp.Pop() / val;
					break;
				}

				sp.Push(val);
			}
		}

		Console.WriteLine("=> {0}", sp.Pop());
	}
}

