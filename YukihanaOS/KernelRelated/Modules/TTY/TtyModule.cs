// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;

namespace YukihanaOS.KernelRelated.Modules
{
#if MOD_TTY
    internal class TtyModule : IKernelModule
    {
        public string Name => nameof(TtyModule);
        public string Description => "Provides kernel with a dynamical and configurable TTY";

        private TTY _ttyInstance;

        public bool Initialize(out string error)
        {
            error = null;
            return true;
        }

        public object SendMessage(params object[] args)
        {
            if (args[0] is uint method)
            {
                switch (method)
                {
                    case 0:     // start TTY
                        return startTty(args[1], args[2], args[3]);
                    case 1:     // print value
                        return print(args[1]);
                    case 2:     // print with new line
                        return printNewLn(args[1]);
                    case 3:     // set color
                        setColor(args[1], args[2]);
                        return true;
                    case 4:     // destroy TTY
                        return destroy();
                    case 5:     // print without updating display buffer
                        return printNoUpd(args[1]);
                    case 6:     // update display buffer maually
                        return updateBuff();
                    case 7:     // get TTY color
                        return getColor(args[1]);
                    case 8:     // set cursor possition
                        return setCurrPos(args[1], args[2]);
                    case 9:     // get component of cursor possition
                        return getCursorPos(args[1]);
                    case 10:    // get component of TTY screen size
                        return getScreenSize(args[1]);
                }
            }

            return false;
        }

        public void Shutdown()
        {
            destroy();
        }

        private object startTty(object x, object y, object font)
        {
            if (x is not uint width)
                return false;
            if (y is not uint height)
                return false;
            if (font is not PCScreenFont fnt)
                return false;

            if (_ttyInstance != null)
                return false;

            _ttyInstance = new TTY(width, height, fnt);

            return true;
        }

        private object print(object value)
        {
            if (_ttyInstance == null)
                return false;

            _ttyInstance.Write(value);
            return true;
        }

        private object printNoUpd(object value)
        {
            if (_ttyInstance == null)
                return false;

            _ttyInstance.WriteNoUpdate(value.ToString());
            return true;
        }

        private object updateBuff()
        {
            if (_ttyInstance == null)
                return false;

            _ttyInstance.Update();

            return true;
        }
        
        private object printNewLn(object value)
        {
            if (_ttyInstance == null)
                return false;

            _ttyInstance.WriteLine(value);
            return true;
        }

        private object setColor(object _side, object consoleColor)
        {
            if (_side is not int side)
                return false;

            if (consoleColor is not ConsoleColor color)
                return false;

            if (_ttyInstance == null)
                return false;

            if (side == 0)
            {
                _ttyInstance.Background = color;
                return true;
            }
            else if(side == 1)
            {
                _ttyInstance.Foreground = color;
                return true;
            }

            return false;
        }

        private object destroy()
        {
            if(_ttyInstance == null)
                return false;

            FullScreenCanvas.Disable();

            return true;
        }

        private object getColor(object side)
        {
            if (side is not int _side)
                return false;

            if (_ttyInstance == null)
                return false;

            if (_side == 0)
                return _ttyInstance.Background;
            else if (_side == 1)
                return _ttyInstance.Foreground;
            else 
                return null;
        }

        private object setCurrPos(object x, object y)
        {
            if (x is not int left)
                return false;
            if (y is not int top)
                return false;

            if(_ttyInstance == null)
                return false;

            _ttyInstance.SetCursorPos(left, top);
            return true;
        }

        private object getCursorPos(object component)
        {
            if (component is not int comp)
                return -1;
            if (_ttyInstance == null)
                return -1;

            if (comp == 0)
                return _ttyInstance.X;
            else if (comp == 1)
                return _ttyInstance.Y;
            else
                return -1;
        }

        private object getScreenSize(object component)
        {
            if (component is not int comp)
                return -1;
            if (_ttyInstance == null)
                return -1;

            if (comp == 0)
                return _ttyInstance.Cols;
            else if (comp == 1)
                return _ttyInstance.Rows;
            else
                return -1;
        }
    }
#endif
}
