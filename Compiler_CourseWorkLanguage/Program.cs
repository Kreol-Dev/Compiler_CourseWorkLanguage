using System;
using System.IO;
using System.Collections.Generic;

namespace Compiler_CourseWorkLanguage
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var lines = File.ReadLines ("example.cwl");
			Stack<int> tabs = new Stack<int> ();

			tabs.Push (0);
			List<string> tokens = new List<string> ();
			tokens.Add ("INDENT");
			foreach (var line in lines) {
				int tabsCount = 0;
				for (int i = 0; i < line.Length; i++) {
					if (line [i] == '\t')
						tabsCount++;
					else
						break;
				}
				string slice = line.Substring (tabsCount);
				bool indent = false;
				if (tabs.Count > 0 && tabsCount == tabs.Peek () + 1) {
					indent = true;
					tabs.Push (tabsCount);
				} else if (tabs.Count > 0) {
					while (tabs.Count > 0 && tabsCount < tabs.Peek()) {
						tabs.Pop ();
						tokens.Add ("DEDENT");
					}
				} else if (tabsCount != tabs.Peek ()) {
					throw new Exception ("Wrong tabulation");
				}
				var lineTokens = slice.Split(' ');
				if (indent)
					tokens.Add ("INDENT");
				foreach (var lineToken in lineTokens)
					tokens.Add (lineToken);


			}
			while (tabs.Count > 0) {
				tabs.Pop ();
				tokens.Add ("DEDENT");
			}
			string text = string.Join (" ", tokens);
	
			Console.WriteLine (text);		
			Console.WriteLine ("---");
			var list = Parser.ParseText (text);
			foreach (var e in list) {
				ShowDef (e);
			}
			Console.ReadLine ();
		}

		static void ShowDef(Definition d, int offset = 0)
		{
			if (d is VarDefinition) {
				VarDefinition vd = d as VarDefinition;
				for (int i = 0; i < offset; i++)
					Console.Write (" ");
				Console.WriteLine ("var " + vd.Type + " " + vd.Name);
			} else if (d is ClassDefinition) {
				ClassDefinition vd = d as ClassDefinition;
				for (int i = 0; i < offset; i++)
					Console.Write (" ");
				Console.WriteLine ("type " + vd.Name + " inherits " + vd.Inherit);
				if (vd.Block != null)
				foreach (var e in vd.Block)
					ShowDef (e, offset + 1);
			} else if (d is FuncDefinition) {
				FuncDefinition vd = d as FuncDefinition;
				for (int i = 0; i < offset; i++)
					Console.Write (" ");
				Console.WriteLine ("func " + vd.Name + " " + vd.Args);

				Console.WriteLine (vd.Block);
			}
		}
	}
}
