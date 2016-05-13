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
			Generate (defines, envFilled, null);
			return envFilled;
		}

		Type FindType(string name)
		{
			return env.GetType (name);
		}
		void Generate(List<Definition> code, CodeTypeDeclaration baseType, CodeTypeDeclaration host)
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
					var defClass = new CodeTypeDeclaration (classDef.Name + "_");
					var classType = FindType (classDef.Inherit);
					if (classType == null)
						classType = typeof(object);
					defClass.BaseTypes.Add (classType);
					defClass.Attributes = isPublic ? MemberAttributes.Public : MemberAttributes.Private;
					baseType.Members.Add (defClass);
					Generate (classDef.Block, defClass, baseType);

				} else if (def is FuncDefinition) {
					
					FuncDefinition funcDef = def as FuncDefinition;

					if ((funcDef.Name + "_") == baseType.Name) {
						CodeConstructor ctor = new CodeConstructor ();
						ctor.Attributes = MemberAttributes.Public;
						foreach (var arg in funcDef.Args) {
							ctor.Parameters.Add (new CodeParameterDeclarationExpression (FindType(arg.Type), arg.Name));
						}
						//defFunc.Attributes |= MemberAttributes.Static;
						//defFunc.ReturnType = baseType;
						//defFunc.ReturnType = new CodeTypeReference (baseType);

						var defFunc = new CodeMemberMethod ();
						defFunc.Name = funcDef.Name;
						var r = new CodeTypeReference (funcDef.ReturnType);
						//defFunc.ReturnType = new CodeTypeReferenceExpression (baseType.Name);
						defFunc.ReturnType = new CodeTypeReference(funcDef.Name + "_");
						foreach (var arg in funcDef.Args) {
							var argType = FindType (arg.Type);
							if(argType != null)
								defFunc.Parameters.Add (new CodeParameterDeclarationExpression (argType, arg.Name));
							else
								defFunc.Parameters.Add (new CodeParameterDeclarationExpression (arg.Type, arg.Name));
						}
						defFunc.Attributes = isPublic ? MemberAttributes.Public : MemberAttributes.Private;
						defFunc.Attributes |= MemberAttributes.Static;
						envFilled.Members.Add (defFunc);


						GenerateCtor (ctor, defFunc, funcDef);

						baseType.Members.Add (ctor);
					} else {
						var defFunc = new CodeMemberMethod ();
						defFunc.Name = funcDef.Name;
						if(funcDef.ReturnType != null)
							defFunc.ReturnType = new CodeTypeReference (FindType (funcDef.ReturnType));
						else
							defFunc.ReturnType = new CodeTypeReference (typeof(void));
						
						foreach (var arg in funcDef.Args) {
							defFunc.Parameters.Add (new CodeParameterDeclarationExpression (FindType(arg.Type), arg.Name));
						}
						defFunc.Attributes = isPublic ? MemberAttributes.Public : MemberAttributes.Private;

						//defFunc.Attributes |= MemberAttributes.
						baseType.Members.Add (defFunc);
						GenerateMethod (defFunc, funcDef);

					}
				} else if (def is VarDefinition) {
					VarDefinition varDef = def as VarDefinition;
					CodeMemberField field = new CodeMemberField ();
					field.Name = varDef.Name;
					var type = FindType (varDef.Type);
					if (type != null)
						field.Type = new CodeTypeReference (type);
					else
						field.Type = new CodeTypeReference (varDef.Type + "_");
					field.Attributes = isPublic ? MemberAttributes.Public : MemberAttributes.Private;
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
					if (type != null)
						snippet = type.ToString () + " " + snippet;
					else
						snippet = varDef.Type + "_ " + snippet;
				}
				var codeStatement = new CodeSnippetStatement (snippet);

				method.Statements.Add (codeStatement);
				Console.WriteLine ("SNIPPET " + snippet);
			}
		}
		void GenerateCtor(CodeConstructor method, CodeMemberMethod hostFunction, FuncDefinition def)
		{
			StringBuilder argsBuilder = new StringBuilder (def.Args.Count * 7);
			foreach (var arg in def.Args)
				argsBuilder.Append (arg.Name).Append(',');
			if(argsBuilder.Length != 0)
			argsBuilder.Length = argsBuilder.Length - 1;
			hostFunction.Statements.Add (new CodeSnippetStatement (String.Format("return new {0}_({1});", def.Name, argsBuilder.ToString())));

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

