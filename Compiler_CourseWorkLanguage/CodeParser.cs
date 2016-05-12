using System;
using Sprache;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Cecil;

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
		
		static readonly Parser<ExprType> And =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("and")
			from rwhite in Parse.WhiteSpace.Many ()
			select ExprType.And;

		static readonly Parser<ExprType> Or =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("or")
			from rwhite in Parse.WhiteSpace.Many ()
			select ExprType.Or;
		static readonly Parser<ExprType> Equal =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("==")
			from rwhite in Parse.WhiteSpace.Many ()
			select ExprType.Equal;
		
		static readonly Parser<UExprType> Not =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("not")
			from rwhite in Parse.WhiteSpace.Many ()
			select UExprType.Not;
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
			from word in Parse.Number.Text()
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
			//from lwhite in Parse.WhiteSpace.Many()
			from first in Parse.Letter
			from rest in Parse.LetterOrDigit.Many ().Text()
			//from rwhite in Parse.WhiteSpace.Many()
			select string.Concat (first, rest);
		
		static readonly Parser<Member> MemberID =
			from ids in Id.XDelimitedBy(Parse.Char ('.'))
			from args in ArgsList.Optional()
			select new Member (){ IDs = new List<string> (ids), CallArgs = args.GetOrElse(null) };

		static readonly Parser<List<Expression>> ArgsList =
			from lbrace in LBrace
			//from exprs in Parse.Ref(()=>Expr).DelimitedBy (Parse.String (",").Token())
			from exprs in ExprList
			from rbrace in RBrace
			select exprs;
		static readonly Parser<List<Expression>> ExprList = 
			from exprs in Parse.Ref (() => Expr).DelimitedBy (Parse.String (",").Token ()).Optional()
			select new List<Expression> (exprs.GetOrElse(null));
//				.XOr(from lbrace in LBrace
//				from rbrace in RBrace
//				select new List<Expression>());
		static readonly Parser<string> OfType =
			from ofWord in Of
			from ofClass in Id.Token()
			select ofClass;
		
		static readonly Parser<List<Definition>> DefBlock =
			from indent in Indent
			from definitions in Parse.Ref(()=>Def).Many()
			from dedent in Dedent
			select new List<Definition>(definitions);

		static readonly Parser<ProtectedDefinition> Def = 
			(from priv in Parse.String ("private").Token ()
				from def in Parse.Ref(()=>ClassDef).Or (Parse.Ref(()=>VarDef)).Or (Parse.Ref(()=>FuncDef))
			 select new ProtectedDefinition (){ IsPublic = false, Definition = def })
				.XOr (from def in Parse.Ref(()=>ClassDef).Or (Parse.Ref(()=>VarDef)).Or (Parse.Ref(()=>FuncDef))
			      select new ProtectedDefinition (){ IsPublic = true, Definition = def });

		
		static readonly Parser<Definition> ClassDef =
			from classWord in Class
			from name in Id.Token()
			from inheritDef in OfType.Optional()
			from block in DefBlock
			select new ClassDefinition(){Name = name, Inherit = inheritDef.GetOrElse("None"), Block = block};
		
		static readonly Parser<Definition> FuncDef =
			from name in Id.Token()
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
//		static readonly Parser<Expression> VarAssign = 
//			from member in MemberID
//			from expr in AssignOp
//			select new VarAssignExpression (){ Member = member as Member, DefaultExpression = expr };
		
		static readonly Parser<List<Statement>> FuncBlock = 
			from indent in Indent
			from definitions in IfThen.Or(WhileLoop).Or(ForLoop).Or(Return).Or<Statement>(AssignExpr).Or(VarDef).Or<Statement>(MemberID).Many()
			from dedent in Dedent
			select new List<Statement>(definitions);

		static readonly Parser<Expression> AssignOp =
			from lwhite in Parse.WhiteSpace.Many ()
			from word in Parse.String ("=")
			from rwhite in Parse.WhiteSpace.Many ()
			from expr in Expr
			select expr;
		
		static readonly Parser<Definition> VarDef =
			from type in Id.Token()
			from name in Id.Token()
			from assign in AssignOp.Optional()
			select new VarDefinition(){Name = name, Type = type, DefaultExpression = assign.GetOrElse(null)};
		
		static readonly Parser<Expression> AssignExpr =
			from member in MemberID
			from expr in AssignOp
			select new VarAssignExpression (){ Member = member, DefaultExpression = expr };
		static readonly Parser<Expression> Expr = 
			Parse.Ref(() => OrOp).XOr (Parse.Ref(() => AndOp)).
			XOr (Parse.Ref(() => EqualOp)).XOr (Parse.Ref(() => AdditOp)).
			XOr (Parse.Ref(() => MultOp)).XOr (Parse.Ref(() => Factor));

		static readonly Parser<Expression> MultOp = Parse.ChainOperator (Div.Or (Mul), Parse.Ref(() => Operand), (op, lexpr, rexpr) => new BinaryExpr(){LExpr = lexpr, RExpr = rexpr, Type = op});
		static readonly Parser<Expression> AdditOp = Parse.ChainOperator (Add.Or (Sub), MultOp, (op, lexpr, rexpr) => new BinaryExpr(){LExpr = lexpr, RExpr = rexpr, Type = op});
		static readonly Parser<Expression> EqualOp = Parse.ChainOperator (Equal, AdditOp, (op, lexpr, rexpr) => new BinaryExpr(){LExpr = lexpr, RExpr = rexpr, Type = op});
		static readonly Parser<Expression> AndOp = Parse.ChainOperator (And, EqualOp, (op, lexpr, rexpr) => new BinaryExpr(){LExpr = lexpr, RExpr = rexpr, Type = op});
		static readonly Parser<Expression> OrOp = Parse.ChainOperator (Or, AndOp, (op, lexpr, rexpr) => new BinaryExpr(){LExpr = lexpr, RExpr = rexpr, Type = op});

		static readonly Parser<Expression> Factor =
			(from lparen in LBrace
				from expr in Parse.Ref(() => Expr)
				from rparen in RBrace
				select new BracedExpression(){InExpr = expr})
				.XOr(Parse.Ref(() => Const));
		static readonly Parser<Expression> Const =
			from expr in Number.XOr(String).XOr(True).XOr(False).XOr(Null)
				//.XOr(Parse.Ref(() => FuncCall))
				.XOr(Parse.Ref(()=>MemberID))
			select expr;
		static readonly Parser<Expression> Operand =
			(from sign in Negate
				from factor in Factor
					select new UnaryExpr(){Type = UExprType.Negate, Expr = factor}
			).XOr(from sign in Not
				from factor in Factor
				select new UnaryExpr(){Type = UExprType.Not, Expr = factor}
			).XOr(Factor).Token();

		static readonly Parser<ElifStatement> Elif = 
			from elifWord in Parse.String ("elif").Token ()
			from ifExpr in Expr
			from elifBlock in FuncBlock
			select new ElifStatement (){ IfExpr = ifExpr, ThenBlock = elifBlock };
		static readonly Parser<List<ElifStatement>> Elifs =
			from elifs in Elif.Many ()
			select new List<ElifStatement> (elifs);
		static readonly Parser<List<Statement>> Else =
			from elseWord in Parse.String("else").Token()
			from elseBlock in FuncBlock
			select elseBlock;
		static readonly Parser<Statement> IfThen =
			from ifWord in Parse.String ("if").Token ()
			from ifExpr in Expr
			from ifBlock in FuncBlock
			from elifs in Elifs
			from elseThen in Else.Optional()
				select new IfThenStatement (){ IfExpr = ifExpr, ThenBlock = ifBlock, Elifs = elifs, ElseBlock = elseThen.GetOrElse(null)};

		static readonly Parser<Statement> WhileLoop = 
			from whileWord in Parse.String ("while").Token ()
			from ifExpr in Expr
			from ifBlock in FuncBlock
			select new WhileStatement (){Expr = ifExpr, Block = ifBlock };

		static readonly Parser<Statement> ForLoop = 
			from whileWord in Parse.String ("foreach").Token ()
			from id in Id
			from inWord in Parse.String("in").Token()
			from expr in Expr
			from block in FuncBlock
			select new ForStatement (){ LoopId = id, InExpr = expr, Block = block };

		static readonly Parser<Statement> Return =
			from retWord in Parse.String ("return").Token ()
			from expr in Expr
			select new ReturnStatement (){ Expression = expr };
		public static List<Definition> ParseText(string text)
		{
			return DefBlock.Parse (text);
		}

	

	}
	public class ProtectedDefinition : Definition
	{
		public bool IsPublic;
		public Definition Definition;
		public override string ToString ()
		{
			if (IsPublic)
				return "public " + Definition.ToString ();
			return Definition.ToString ();
		}
	}
	public class ReturnStatement : Statement
	{
		public Expression Expression;
		public override string ToString ()
		{
			return "return " + Expression.ToString () + ";";
		}
	}
	public class IfThenStatement : Statement
	{
		public Expression IfExpr;
		public List<Statement> ThenBlock;
		public List<ElifStatement> Elifs;
		public List<Statement> ElseBlock;
		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder (200);
			builder.Append ("if (");
			builder.Append (IfExpr);
			builder.Append (") {");
			builder.Append (Environment.NewLine);
			foreach (var stmt in ThenBlock)
				builder.Append("   ").Append (stmt).Append(Environment.NewLine);
			builder.Append ("}");
			foreach (var elif in Elifs)
				builder.Append (elif);
			if (ElseBlock == null)
				return builder.ToString ();
			builder.Append ("else {").Append(Environment.NewLine);
			foreach (var stmt in ElseBlock)
				builder.Append("   ").Append (stmt).Append(Environment.NewLine);

			builder.Append ("}");
			return builder.ToString ();

		}
	}
	public class WhileStatement : Statement
	{
		public Expression Expr;
		public List<Statement> Block;
		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder (200);
			builder.Append ("while ( ");
			builder.Append (Expr);
			builder.Append (" ) {");
			builder.Append (Environment.NewLine);
			foreach (var stmt in Block)
				builder.Append("   ").Append (stmt).Append(Environment.NewLine);
			builder.Append ("}");
			return builder.ToString ();
		}
	
	}
	public class ForStatement : Statement
	{
		public string LoopId;
		public Expression InExpr;
		public List<Statement> Block;
		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder (200);
			builder.Append ("foreach ( var ");
			builder.Append (LoopId);
			builder.Append (" in ");
			builder.Append (InExpr);
			builder.Append (") {");
			builder.Append (Environment.NewLine);
			foreach (var stmt in Block)
				builder.Append("   ").Append (stmt).Append(Environment.NewLine);
			builder.Append ("}");
			return builder.ToString ();
		}

	}
	public class ElifStatement : Statement
	{
		public Expression IfExpr;
		public List<Statement> ThenBlock;
		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder (100);
			builder.Append ("else if ( ");
			builder.Append (IfExpr);
			builder.Append (") {");
			builder.Append (Environment.NewLine);
			foreach (var stmt in ThenBlock)
				builder.Append("   ").Append (stmt).Append(Environment.NewLine);
			builder.Append ("}");
			return builder.ToString ();
		}
	}
	public class FuncCall : Expression
	{
		public Member Member;
		public List<Expression> Args;

	}
	public class Member : Expression
	{
		StringBuilder builder = new StringBuilder();
		public List<string> IDs;
		public List<Expression> CallArgs;
		public override string ToString ()
		{
			builder.Clear ();
			for (int i = 0; i < IDs.Count - 1; i++)
				builder.Append (IDs [i]).Append (".");
			builder.Append (IDs [IDs.Count - 1]);
			
			if (CallArgs != null) {
				if (!(CallArgs.Count == 1 && CallArgs [0] is Member && (CallArgs[0] as Member).IDs[0] == "void")) {
					builder.Append (" (");
					for (int i = 0; i < CallArgs.Count; i++)
						builder.Append (CallArgs [i]).Append ("     ");
					builder.Append (")");
				}
			}
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
			if (DefaultExpression == null)
				return Name + ";";
			return String.Format ("{0} = {1};", Name, DefaultExpression);
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
			if (DefaultExpression == null)
				return Member.ToString() + ";";
			return String.Format ("{0} = {1};", Member, DefaultExpression);
		}

	
	}

	public abstract class Statement
	{
		
	}

	public abstract class Expression : Statement
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
			case ExprType.And:
				return string.Format("{0} and {1}", LExpr, RExpr);
			case ExprType.Or:
				return string.Format("{0} or {1}", LExpr, RExpr);			
			case ExprType.Equal:
				return string.Format("{0} == {1}", LExpr, RExpr);
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
		Div,
		And,
		Or,
		Equal
	}
	public enum UExprType
	{
		Negate,
		Not
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

