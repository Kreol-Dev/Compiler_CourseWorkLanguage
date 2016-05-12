using System;

namespace Compiler_CourseWorkLanguage
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
			return typeof(object);
		}
	}
}

