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
using System.Reflection;

namespace Compiler_CourseWorkLanguage
{
	class MainClass
	{
		
		public static void Main (string[] args)
		{
			CWL<BasicEnv> cwl = new CWL<BasicEnv> ();
			cwl.Load ("example.cwl");
			cwl.Compile ();
			Console.WriteLine("Test callback: " + cwl.Env.TestCallback (2));

					


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
