using System;
using Compiler_CourseWorkLanguage;


public interface IConsole
{
	void WriteLine (object obj);
}
public class EnvConsole : IConsole
{
	public void WriteLine(object obj)
	{
		System.Console.WriteLine (obj);
	}

}
namespace Envs
{
	public class BasicEnv: IEnvironment
	{
		public IConsole console { get; internal set; }
		public BasicEnv ()
		{
			console = new EnvConsole();
		}


		public Func<float, string> TestCallback;
		public Type GetType(string name)
		{
			if (name == "number")
				return typeof(float);
			else if (name == "string")
				return typeof(string);
			else if (name == "object")
				return typeof(object);
			return null;
		}

		public string ReadLine()
		{
			return System.Console.ReadLine ();
		}
	}
}

