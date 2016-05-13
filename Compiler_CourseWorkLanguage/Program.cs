using System;
using System.IO;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using ICSharpCode.Decompiler;
using Mono.Cecil;
using ICSharpCode.Decompiler.Ast;
using System.Linq;
using System.CodeDom;
using Microsoft.CSharp;
using Envs;

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
			tokens.Add (CodeParser.INDENT);
			foreach (var unprepLine in lines) {
				string line = unprepLine;
//				bool allSpace = true;
//				int lastBraceOpen = -1;
//				for (int i = 0; i < unprepLine.Length; i++) {
//					if (unprepLine [i] == '(') {
//						allSpace = true;
//						lastBraceOpen = i;
//					}
//					else if (unprepLine [i] == ')') {
//						
//							if (allSpace == true) {
//								line = String.Concat (unprepLine.Substring (0, i), " void " ,unprepLine.Substring (i, unprepLine.Length - i));
//							}
//					} else if (unprepLine [i] != ' ')
//						allSpace = false;
//				}

				int tabsCount = 0;
				for (int i = 0; i < line.Length; i++) {
					if (line [i] == '\t')
						tabsCount++;
					else
						break;
				}
				string slice = line.Substring (tabsCount);
				bool empty = true;
				for (int i = 0; i < slice.Length && empty; i++) {
					if (slice [i] != ' ' || slice [i] != '\t')
						empty = false;
				}
				if (empty)
					continue;
				bool indent = false;
				if (tabs.Count > 0 && tabsCount == tabs.Peek () + 1) {
					indent = true;
					tabs.Push (tabsCount);
				} else if (tabs.Count > 0) {
					while (tabs.Count > 0 && tabsCount < tabs.Peek()) {
						tabs.Pop ();
						tokens.Add (CodeParser.DEDENT);
					}
				} else if (tabsCount != tabs.Peek ()) {
					throw new Exception ("Wrong tabulation");
				}
				var lineTokens = slice.Split(' ');
				if (indent)
					tokens.Add (CodeParser.INDENT);
				foreach (var lineToken in lineTokens)
					if (lineToken.Length > 0)
						tokens.Add (lineToken);


			}
			while (tabs.Count > 0) {
				tabs.Pop ();
				tokens.Add (CodeParser.DEDENT);
			}
			string text = string.Join (" ", tokens);
	
			Console.WriteLine (text);		
			Console.WriteLine ("---");
			var list = CodeParser.ParseText (text);
			foreach (var e in list) {
				ShowDef (e);
			}

			DomGenerator gen = new DomGenerator (new BasicEnv());
			var genClass = gen.Define (list);
			CodeCompileUnit pUnit = new CodeCompileUnit();
//			foreach(string sUsing in typeof(BasicEnv)) pNamespace.Imports.Add(new
//				CodeNamespaceImport(sUsing));
			CodeNamespace pNamespace = new CodeNamespace("Scripts");
			pNamespace.Types.Add(genClass);
			pUnit.Namespaces.Add(pNamespace);
			pUnit.ReferencedAssemblies.Add ("Compiler_CourseWorkLanguage.exe");
			pNamespace.Imports.Add (new CodeNamespaceImport ("Envs"));
			CompilerParameters pParams = new CompilerParameters ();
			pParams.GenerateInMemory = true;

			CodeDomProvider provider = new CSharpCodeProvider ();
			var writer = Console.Out;
			var sourcegen = provider.CreateGenerator (writer);
			sourcegen.GenerateCodeFromCompileUnit (pUnit, writer, new CodeGeneratorOptions ());
			CompilerResults results = provider.CompileAssemblyFromDom (pParams, pUnit);

			if (results.Errors != null && results.Errors.Count > 0) {
				foreach ( var err in results.Errors)
					Console.WriteLine("Error: " + err);
			
			}

			Console.ReadLine ();
		}

		static void ShowDef(Definition d, int offset = 0)
		{
			if (d is ProtectedDefinition) {
				ProtectedDefinition def = d as ProtectedDefinition;
				if (def.IsPublic)
					Console.Write ("public ");
				else
					Console.Write ("private ");
				ShowDef (def.Definition);
			}
			if (d is VarDefinition) {
				VarDefinition vd = d as VarDefinition;
				for (int i = 0; i < offset; i++)
					Console.Write (" ");
				Console.WriteLine (vd.ToString());
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
				Console.Write ("func " + vd.Name + " " + vd.ReturnType + " args:   ");
				foreach ( var arg in vd.Args)
					Console.Write (arg.Name + " of " + arg.Type + "   ");
				Console.WriteLine ();
				foreach (var stmt in vd.Block) {
					for (int i = 0; i < offset + 1; i++)
					Console.Write (" ");
					Console.WriteLine (stmt.ToString());
				}
					
			}
		}
	}
}
