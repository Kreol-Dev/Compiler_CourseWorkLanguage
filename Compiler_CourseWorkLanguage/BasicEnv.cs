using System;
using Compiler_CourseWorkLanguage;

namespace Envs
{
	public class BasicEnv: IEnvironment
	{
		public BasicEnv ()
		{
		}

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
	}
}

