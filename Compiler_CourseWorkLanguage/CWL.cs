using System;
using System.Collections.Generic;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

namespace Compiler_CourseWorkLanguage
{
	public interface IEnvironment
	{
		Type GetType(string name);
	}
	public class CWL<T> where T  : class, IEnvironment, new()
	{
		public T Env { get; internal set; }
		DomGenerator gen;
		CodeCompileUnit pUnit;
		CompilerParameters pParams;
		CodeDomProvider provider;
		CodeNamespace pNamespace;
		CodeTypeDeclaration genClass;
		public CWL()
		{
			var env = new T ();
			gen = new DomGenerator (env);
			genClass = gen.envFilled;
			pUnit = new CodeCompileUnit();
			//			foreach(string sUsing in typeof(BasicEnv)) pNamespace.Imports.Add(new
			//				CodeNamespaceImport(sUsing));
			pNamespace = new CodeNamespace("Scripts");
			pNamespace.Types.Add(genClass);
			pUnit.Namespaces.Add(pNamespace);
			pUnit.ReferencedAssemblies.Add ("Compiler_CourseWorkLanguage.exe");
			pNamespace.Imports.Add (new CodeNamespaceImport ("Envs"));
			pParams = new CompilerParameters ();
			pParams.GenerateInMemory = true;

			provider = new CSharpCodeProvider ();
			//var writer = Console.Out;
			//var sourcegen = provider.CreateGenerator (writer);
			//sourcegen.GenerateCodeFromCompileUnit (pUnit, writer, new CodeGeneratorOptions ());

		}

		public void Load(string filePath)
		{
			var lines = File.ReadLines (filePath);
			Stack<int> tabs = new Stack<int> ();




			tabs.Push (0);
			List<string> tokens = new List<string> ();
			tokens.Add (CodeParser.INDENT);
			foreach (var unprepLine in lines) {
				string line = unprepLine;
				bool allSpace = true;
				int lastBraceOpen = -1;
				for (int i = 0; i < unprepLine.Length; i++) {
					if (unprepLine [i] == '(') {
						allSpace = true;
						lastBraceOpen = i;
					}
					else if (unprepLine [i] == ')') {

						if (allSpace == true) {
							line = String.Concat (unprepLine.Substring (0, i), " void " ,unprepLine.Substring (i, unprepLine.Length - i));
						}
					} else if (unprepLine [i] != ' ')
						allSpace = false;
				}

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
//			foreach (var e in list) {
//				ShowDef (e);
//			}

			gen.Define (list);

		}

		public void Compile()
		{
			CompilerResults results = provider.CompileAssemblyFromDom (pParams, pUnit);


			if (results.Errors != null && results.Errors.Count > 0) {
				//				foreach ( var err in results.Errors)
				//					Console.WriteLine("Error: " + err);
				Console.WriteLine("ERROR");
			}

			var filledEnvType = results.CompiledAssembly.GetType (pNamespace.Name + "." + genClass.Name);
			Dictionary<string, FieldInfo> callbacks = new Dictionary<string, FieldInfo> ();
			Env = Activator.CreateInstance(filledEnvType) as T;
			foreach (var field in filledEnvType.GetFields()) {
				if (field.Name.EndsWith ("Callback")) {
					callbacks.Add (field.Name.Substring(0, field.Name.Length - 8), field);
				}
			}
			foreach (var member in filledEnvType.GetMethods()) {
				FieldInfo callback = null;
				if (callbacks.TryGetValue (member.Name, out callback)) {
					callback.SetValue (Env, Delegate.CreateDelegate(callback.FieldType, Env, member));
				}
			}
		}
	}
}

