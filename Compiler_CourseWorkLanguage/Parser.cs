using System;
using Sprache;
using System.Collections.Generic;
using System.Linq;

namespace Compiler_CourseWorkLanguage
{
	public static class Parser
	{
		static readonly Parser<string> Indent =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("INDENT").Text()
			from rwhite in Parse.WhiteSpace.Many ()
			select word;
		
		static readonly Parser<string> Dedent =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("DEDENT").Text()
			from rwhite in Parse.WhiteSpace.Many ()
			select word;
		
		static readonly Parser<string> Class =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("class").Text()
			from rwhite in Parse.WhiteSpace.Many ()
			select word;
		
		static readonly Parser<string> Of =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("of").Text()
			from rwhite in Parse.WhiteSpace.Many ()
			select word;
		
		static readonly Parser<string> LBrace =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("(").Text()
			from rwhite in Parse.WhiteSpace.Many ()
			select word;
		static readonly Parser<string> RBrace =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String (")").Text()
			from rwhite in Parse.WhiteSpace.Many ()
			select word;
		static readonly Parser<string> Id =
			from lwhite in Parse.WhiteSpace.Many()
			from first in Parse.Letter
			from rest in Parse.LetterOrDigit.Many ().Text()
			from rwhite in Parse.WhiteSpace.Many()
			select string.Concat (first, rest);
		
		static readonly Parser<string> InheritDef =
			from ofWord in Of
			from ofClass in Id
			select ofClass;
		
		static readonly Parser<List<Definition>> DefBlock =
			from indent in Indent
			from definitions in VarDef.Or(ClassDef).Or(FuncDef).Many()
			from dedent in Dedent
			select new List<Definition>(definitions);

		static readonly Parser<Definition> ClassDef =
			from classWord in Class
			from name in Id
			from inheritDef in InheritDef.Optional()
			from block in DefBlock
			select new ClassDefinition(){Name = name, Inherit = inheritDef.GetOrElse("None"), Block = block};
		
		static readonly Parser<Definition> FuncDef =
			from type in Id.Optional()
			from name in Id
			from lBrace in LBrace
			from paramsList in Parse.CharExcept(')').Many().Text()
			from rBrace in RBrace
			select new FuncDefinition(){
			ReturnType = type.GetOrElse(null), 
			Name = name, Args = paramsList
			};
		
		static readonly Parser<Definition> VarDef =
			from name in Id
			from ofWord in Of
			from type in Id
			select new VarDefinition(){Name = name, Type = type};
		
		public static List<Definition> ParseText(string text)
		{
			return DefBlock.Parse (text);
		}

	

	}

	public class Result<T>
	{
		public T Content { get; internal set; }
		public string Other { get; internal set; }
		public Result(T content, string other)
		{
			Content = content;
			Other = other;
		}

	}
	public class ClassDefinition : Definition
	{
		public string Inherit;
		public List<Definition> Block;
	}

	public class FuncDefinition : Definition
	{
		public string ReturnType;
		public string Args;
		public string Block;
	}	
	public class Definition
	{
		public string Name;
	}
	public class VarDefinition : Definition
	{
		public string Type;
	}
}

