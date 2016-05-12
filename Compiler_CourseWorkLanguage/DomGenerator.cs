using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.CodeDom;

namespace Compiler_CourseWorkLanguage
{

	public interface IEnvironment
	{
		Type GetType(string name);
	}
	public class DomGenerator
	{
		IEnvironment env;
		CodeTypeDeclaration envFilled;
		public DomGenerator (IEnvironment environment)
		{
			env = environment;
			envFilled = new CodeTypeDeclaration (env.GetType ().Name + "_Filled");
			envFilled.BaseTypes.Add (new CodeTypeReference (env.GetType ()));
		}

		public CodeTypeDeclaration Define(List<Definition> defines)
		{
			Generate (defines, envFilled);
			return envFilled;
		}

		Type FindType(string name)
		{
			return typeof(object);
		}
		void Generate(List<Definition> code, CodeTypeDeclaration baseType)
		{
			for (int i = 0; i < code.Count; i++) {
				Definition def = code [i];
				bool isPublic = true;
				if (def is ProtectedDefinition) {
					ProtectedDefinition pDef = def as ProtectedDefinition;
					def = pDef.Definition;
					isPublic = pDef.IsPublic;
				}

				if (def is ClassDefinition) {
					ClassDefinition classDef = def as ClassDefinition;
					var defClass = new CodeTypeDeclaration (classDef.Name);
					defClass.BaseTypes.Add (FindType (classDef.Inherit));
					defClass.Attributes = isPublic ? MemberAttributes.Public : MemberAttributes.Private;
					baseType.Members.Add (defClass);
					Generate (classDef.Block, defClass);

				} else if (def is FuncDefinition) {
					
					FuncDefinition funcDef = def as FuncDefinition;
					var defFunc = new CodeMemberMethod ();
					defFunc.Name = funcDef.Name;
					defFunc.ReturnType = new CodeTypeReference (FindType (funcDef.ReturnType));
					foreach (var arg in funcDef.Args) {
						defFunc.Parameters.Add (new CodeParameterDeclarationExpression (FindType(arg.Type), arg.Name));
					}
					defFunc.Attributes = isPublic ? MemberAttributes.Public : MemberAttributes.Private;
					baseType.Members.Add (defFunc);
					GenerateMethod (defFunc, funcDef);
				} else if (def is VarDefinition) {
					VarDefinition varDef = def as VarDefinition;
					CodeMemberField field = new CodeMemberField ();
					field.Name = varDef.Name;
					field.Type = new CodeTypeReference (FindType (varDef.Type));
					GenerateVariable (field, varDef);
					baseType.Members.Add (field);
				}
			


			}

		}

		void GenerateMethod(CodeMemberMethod method, FuncDefinition def)
		{
			foreach (var stmt in def.Block) {
				string snippet = stmt.ToString ();
				if (stmt is VarDefinition) {
					var varDef = stmt as VarDefinition;
					Type type = FindType (varDef.Type);
					snippet = type.ToString () + " " + snippet;
				}
				var codeStatement = new CodeSnippetStatement (snippet);

				method.Statements.Add (codeStatement);
				Console.WriteLine ("SNIPPET " + snippet);
			}
		}

		void GenerateVariable(CodeMemberField field, VarDefinition def)
		{
			if (def.DefaultExpression != null)
			field.InitExpression = new CodeSnippetExpression (def.DefaultExpression.ToString ());
		}

			
	}


}

