using System;
using Sprache;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler_CourseWorkLanguage
{
	public static class CodeParser
	{
		static readonly Parser<ExprType> Add =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("+")
			from rwhite in Parse.WhiteSpace.Many ()
			select ExprType.Add;

		static readonly Parser<ExprType> Sub =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("-")
			from rwhite in Parse.WhiteSpace.Many ()
			select ExprType.Sub;

		static readonly Parser<UExprType> Negate =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("-")
			from rwhite in Parse.WhiteSpace.Many ()
			select UExprType.Negate;

		static readonly Parser<ExprType> Mul =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("*")
			from rwhite in Parse.WhiteSpace.Many ()
			select ExprType.Mul;

		static readonly Parser<ExprType> Div =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("/")
			from rwhite in Parse.WhiteSpace.Many ()
			select ExprType.Div;

		static readonly Parser<Expression> True =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("true")
			from rwhite in Parse.WhiteSpace.Many ()
			select new BoolExpression(){Value = true};
		static readonly Parser<Expression> False =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("false")
			from rwhite in Parse.WhiteSpace.Many ()  
			select new BoolExpression(){Value = false};
		static readonly Parser<Expression> Null =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("null").Text()
			from rwhite in Parse.WhiteSpace.Many ()
			select new RefExpression(){Value = null};

		static readonly Parser<Expression> Number =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.Decimal.Text()
			from rwhite in Parse.WhiteSpace.Many ()
			select new NumberExpression(){Value = float.Parse(word)};
		static readonly Parser<Expression> String =
			from lwhite in Parse.WhiteSpace.Many ()
			from lquote in Parse.Char('\'')
			from word in Parse.CharExcept('\'').Many().Text()
			from rquote in Parse.Char('\'')
			from rwhite in Parse.WhiteSpace.Many ()
			select new StringExpression(){Value = word};
		
		public const string INDENT = "{";
		public const string DEDENT = "}";
		static readonly Parser<string> Indent =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String (INDENT).Text()
			from rwhite in Parse.WhiteSpace.Many ()
			select word;
		
		static readonly Parser<string> Dedent =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String (DEDENT).Text()
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
		
		static readonly Parser<Expression> MemberID =
			from ids in Id.DelimitedBy (Parse.Char ('.'))
			select new Member (){ IDs = new List<string> (ids) };
		static readonly Parser<string> OfType =
			from ofWord in Of
			from ofClass in Id
			select ofClass;
		
		static readonly Parser<List<Definition>> DefBlock =
			from indent in Indent
			from definitions in ClassDef.Or(VarDef).Or(FuncDef).Many()
			from dedent in Dedent
			select new List<Definition>(definitions);

		static readonly Parser<Definition> ClassDef =
			from classWord in Class
			from name in Id
			from inheritDef in OfType.Optional()
			from block in DefBlock
			select new ClassDefinition(){Name = name, Inherit = inheritDef.GetOrElse("None"), Block = block};
		
		static readonly Parser<Definition> FuncDef =
			from name in Id
			from lBrace in LBrace
			from paramsList in VarDef.Select( d => d as VarDefinition).Many().Optional()
			from rBrace in RBrace
			from type in OfType.Optional()
			from block in FuncBlock
			select new FuncDefinition(){
			ReturnType = type.GetOrElse(null), 
			Name = name, Args = new List<VarDefinition>(paramsList.GetOrElse(new List<VarDefinition>())),
			Block = block
		};
		static readonly Parser<Expression> VarAssign = 
			from member in MemberID
			from expr in AssignOp
			select new VarAssignExpression (){ Member = member as Member, DefaultExpression = expr };
		static readonly Parser<List<Statement>> FuncBlock = 
			from indent in Indent
			from definitions in VarDef.Or<Statement>(VarAssign).Many()
			from dedent in Dedent
			select new List<Statement>(definitions);

		static readonly Parser<Expression> AssignOp =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("=")
			from rwhite in Parse.WhiteSpace.Many ()
			from expr in AnyExpr
			select expr;
		static readonly Parser<Expression> Const =
			from expr in Number.Or(String).Or(True).Or(False).Or(Null)
			select expr;

		static readonly Parser<Expression> AnyExpr = 
			from expr in Parse.Ref(() => Expr).Or(Parse.Ref(() => TermExpr)).Or(Parse.Ref(() => NegateExpr))
			select expr;
		
		static readonly Parser<Expression> FactorExpr = 
			(from lparen in LBrace
				from expr in Parse.Ref(() => Expr).Or(Parse.Ref(() => TermExpr)).Or(Parse.Ref(() => NegateExpr))
				from rparen in RBrace
				select new BracedExpression(){InExpr = expr})
				.XOr(Const);
				//.XOr(Function);

		static readonly Parser<Expression> NegateExpr = 
			(from sign in Negate
				from factor in FactorExpr
				select new UnaryExpr(){Type = UExprType.Negate, Expr = factor}
			).XOr (FactorExpr);

		static readonly Parser<Expression> TermExpr =
			from lexpr in NegateExpr.Or(Parse.Ref(() => TermExpr))
			from operand in Mul.Or(Div)
			from rexpr in Parse.Ref(() => TermExpr).Or(FactorExpr)
			select new BinaryExpr(){LExpr = lexpr, RExpr = rexpr, Type = operand};
		
		static readonly Parser<Expression> Expr =
			from lexpr in Const.Or(TermExpr).Or(FactorExpr).Or(NegateExpr).Or(Parse.Ref(() => Expr))
			from operand in Add.Or(Sub)
			from rexpr in Parse.Ref(() => Expr).Or(TermExpr).Or(FactorExpr)
			select new BinaryExpr(){LExpr = lexpr, RExpr = rexpr, Type = operand};
		
		static readonly Parser<Definition> VarDef =
			from type in Id
			from name in Id
			from assign in AssignOp.Optional()
			select new VarDefinition(){Name = name, Type = type, DefaultExpression = assign.GetOrElse(null)};
		
		public static List<Definition> ParseText(string text)
		{
			return DefBlock.Parse (text);
		}

	

	}
	public class Member : Expression
	{
		static StringBuilder builder = new StringBuilder();
		public List<string> IDs;
		public override string ToString ()
		{
			builder.Clear ();
			for (int i = 0; i < IDs.Count; i++)
				builder.Append (IDs [i]).Append (".");
			return builder.ToString ();
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
		public List<VarDefinition> Args;
		public List<Statement> Block;
	}	
	public class Definition : Statement
	{
		public string Name;
	}
	public class VarDefinition : Definition
	{
		public string Type;
		public Expression DefaultExpression;
		public override string ToString ()
		{
			return String.Format ("{0} {1} = {2}", Type, Name, DefaultExpression);
		}
	}

	public class VarAssign : Statement
	{
		public string Name;
		public Expression DefaultExpression;
	}

	public class VarAssignExpression : Expression
	{
		public Member Member;
		public Expression DefaultExpression;
		public override string ToString ()
		{
			return String.Format ("{0} = {1}", Member, DefaultExpression);
		}
	}

	public class Statement
	{
	}

	public class Expression : Statement
	{

	}


	public class BinaryExpr : Expression
	{
		public Expression LExpr;
		public Expression RExpr;
		public ExprType Type;
		public override string ToString ()
		{
			switch (Type) {
			case ExprType.Add:
				return string.Format("{0} + {1}", LExpr, RExpr);
			case ExprType.Sub:
				return string.Format("{0} - {1}", LExpr, RExpr);
			case ExprType.Div:
				return string.Format("{0} / {1}", LExpr, RExpr);
			case ExprType.Mul:
				return string.Format("{0} * {1}", LExpr, RExpr);
			}
			return "null_bin_expr";
		}
	}

	public class UnaryExpr : Expression
	{
		public Expression Expr;
		public UExprType Type;
		public override string ToString ()
		{
			return "-" + Expr.ToString ();
		}
	}

	public enum ExprType
	{
		Add,
		Sub,
		Mul,
		Div
	}
	public enum UExprType
	{
		Negate
	}

	public class BoolExpression : Expression
	{
		public bool Value;
		public override string ToString ()
		{
			return Value.ToString();
		}
	}

	public class NumberExpression : Expression
	{
		public float Value;
		public override string ToString ()
		{
			return Value.ToString();
		}
	}

	public class StringExpression : Expression
	{
		public string Value;
		public override string ToString ()
		{
			return Value;
		}
	}

	public class RefExpression : Expression
	{
		public string Value;
		public override string ToString ()
		{
			return Value;
		}
	}

	public class BracedExpression : Expression
	{
		public Expression InExpr;
		public override string ToString ()
		{
			return "(" + InExpr.ToString () + ")";
		}
	}

}

