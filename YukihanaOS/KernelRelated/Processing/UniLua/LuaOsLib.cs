
namespace UniLua
{
    internal class LuaOSLib
    {
		public const string LIB_NAME = "os";

		public static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair("clock", 	OS_Clock),
				new NameFuncPair("execute", OS_Execute),
				new NameFuncPair("getenv",  OS_Getenv),
			};

			lua.L_NewLib( define );
			return 1;
		}

		private static int OS_Clock( ILuaState lua )
		{
            //lua.PushNumber((DateTime.Now - CommandManager.LastLuaStart).TotalSeconds);
            lua.PushNumber(0);
			return 1;
		}

		private static int OS_Execute( ILuaState lua )
		{
			var input = lua.L_CheckString(1);
			//Sys.CommandManager.ProcessInput( input );
			return 1;
		}

		private static int OS_Getenv( ILuaState lua )
		{
			string key = lua.L_CheckString(1);

			//if(Environment.HasProcessEnvValue(-1, key))
			//{
			//	lua.PushString(Registry.Environment.GetProcessEnvValue(-1, key));
			//}
			//else if(Registry.Environment.HasValue(key))
			//{
			//	lua.PushString(Registry.Environment.GetValue(key));
			//}
            //else
            //{
				lua.PushNil();
            //}
            return 1;
		}
	}
}

