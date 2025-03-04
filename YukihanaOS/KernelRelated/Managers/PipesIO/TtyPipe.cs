// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using System.Text;
using YukihanaOS.KernelRelated.Modules;

namespace YukihanaOS.KernelRelated.Managers.PipesIO
{

#if MOD_TTY
    internal class TtyPipe : IPipeIO
    {
        public void Write(char value)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, value);
        }

        public void Write(char[] buffer)
        {
            for(int i = 0; i < buffer.Length; i++)
            {
                Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)5, buffer[i]);
            }
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)6);
        }

        public void Write(char[] buffer, int idx, int count)
        {
            for (int i = idx; i < count; i++)
            {
                Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)5, buffer[i]);
            }
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)6);
        }

        public void Write(bool value)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, value ? "true" : "false");
        }

        public void Write(object value)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, value);
        }

        public void Write(string value)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, value);
        }

        public void Write(StringBuilder value)
        {
            if (value == null)
                return;

            foreach(ReadOnlyMemory<char> chunk in value.GetChunks())
            {
                Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)5, chunk.Span.ToArray());
            }
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)6);
        }

        public void Write(string format, object arg0)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, string.Format(format, arg0));
        }

        public void Write(string format, object arg0, object arg1)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, string.Format(format, arg0, arg1));
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, string.Format(format, arg0, arg1, arg2));
        }

        public void Write(string format, params object[] arg)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, string.Format(format, arg));
        }

        private void WriteNewLine() => Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, '\n');

        public void WriteLine(char value)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)2, value);
        }

        public void WriteLine(char[] buffer)
        {
            foreach (char ch in buffer)
            {
                Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)5, ch);
            }
            WriteNewLine();
        }

        public void WriteLine(char[] buffer, int idx, int count)
        {
            for (int i = idx; i < count; i++)
            {
                Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)5, buffer[i]);
            }
            WriteNewLine();
        }

        public void WriteLine(bool value)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)2, value ? "true" : "false");
        }

        public void WriteLine(object value)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)2, value);
        }

        public void WriteLine(string value)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)2, value);
        }

        public void WriteLine(StringBuilder value)
        {
            if (value == null)
                return;

            foreach (ReadOnlyMemory<char> chunk in value.GetChunks())
            {
                Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)5, chunk.Span.ToArray());
            }
            WriteNewLine();
        }

        public void WriteLine(string format, object arg0)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)2, string.Format(format, arg0));
        }

        public void WriteLine(string format, object arg0, object arg1)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)2, string.Format(format, arg0, arg1));
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)2, string.Format(format, arg0, arg1, arg2));
        }

        public void WriteLine(string format, params object[] arg)
        {
            Kernel.ModuleManager.SendModuleMessage(nameof(TtyModule), out _, (uint)1, string.Format(format, arg));
        }
    }
#endif
}
