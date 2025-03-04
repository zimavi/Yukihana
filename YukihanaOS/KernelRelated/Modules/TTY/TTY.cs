// Yukihana OS 2025 Yukihana OS Contributors
// Licensed under the GPL-3.0 License. See LICENSE for details.

using System;
using System.Collections.Generic;
using System.Drawing;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using YukihanaOS.KernelRelated.Resources;

namespace YukihanaOS.KernelRelated.Modules
{
#if MOD_TTY
    public struct Cell
    {
        public char Char;
        public uint Foreground;
        public uint Background;
    }
    public class TTY
    {
        private const char _NEW_LINE = '\n';
        private const char _CARRIAGE = '\r';
        private const char _TAB      = '\t';
        private const char _BACK     = '\b';
        private const char _SPACE    = ' ';


        private uint[] _pallete = new uint[16];
        private Cell[] _text;
        private List<Cell[]> _textHistory;
        private int _textHistoryIdx = 0;

        private uint _foreground = (byte)ConsoleColor.White;
        private uint _background = (byte)ConsoleColor.Black;

        public Canvas Canvas;

        private Font _font;


        public bool ScrollMode = false;
        public bool CursorVisible;

        public int X, Y = 0;

        public int Cols, Rows = 0;

        public bool StoreCommandOutput = false;

        public Mode ScreenMode => Canvas.Mode;

        public Color ForegroundColor = Color.White;

        public ConsoleColor Foreground
        {
            get => (ConsoleColor)_foreground;
            set
            {
                _foreground = (uint)value;

                uint color = _pallete[_foreground];
                byte r = (byte)(color >> 16 & 0xff);
                byte g = (byte)(color >> 8 & 0xff);
                byte b = (byte)(color & 0xff);

                ForegroundColor = Color.FromArgb(0xFF, r, g, b);
            }
        }

        public Color BackgroundColor = Color.Black;

        public ConsoleColor Background
        {
            get => (ConsoleColor)_background;
            set
            {
                _background = (uint)value;

                uint color = _pallete[_background];
                byte r = (byte)(color >> 16 & 0xff);
                byte g = (byte)(color >> 8 & 0xff);
                byte b = (byte)(color & 0xff);

                BackgroundColor = Color.FromArgb(0xFF, r, g, b);
            }
        }


        public void Update()
        {
            Canvas.Clear(Color.Black);
            
            for(int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    int idx = GetIndex(i, j);

                    if (_text[idx].Char == 0)
                        continue;

                    if (_text[idx].Char == '\n')
                        break;

                    Canvas.DrawFilledRectangle(Color.FromArgb((int)_text[idx].Background), 0 + j * _font.Width, 0 + i * _font.Height,
                        _font.Width, _font.Height);

                    Canvas.DrawChar(_text[idx].Char, _font,
                        Color.FromArgb((int)_text[idx].Foreground), 0 + j * _font.Width, 0 + i * _font.Height);
                }
            }

            Canvas.Display();
        }

        public TTY(uint x, uint y, PCScreenFont fontToUse)
        {
            Canvas = FullScreenCanvas.GetFullScreenCanvas(new Mode(x, y, ColorDepth.ColorDepth32));

            _pallete[0] = 0xFF000000; // Black
            _pallete[1] = 0xFF0000AB; // Darkblue
            _pallete[2] = 0xFF008000; // DarkGreen
            _pallete[3] = 0xFF008080; // DarkCyan
            _pallete[4] = 0xFF800000; // DarkRed
            _pallete[5] = 0xFF800080; // DarkMagenta
            _pallete[6] = 0xFF808000; // DarkYellow
            _pallete[7] = 0xFFC0C0C0; // Gray
            _pallete[8] = 0xFF808080; // DarkGray
            _pallete[9] = 0xFF5353FF; // Blue
            _pallete[10] = 0xFF55FF55; // Green
            _pallete[11] = 0xFF00FFFF; // Cyan
            _pallete[12] = 0xFFAA0000; // Red
            _pallete[13] = 0xFFFF00FF; // Magenta
            _pallete[14] = 0xFFFFFF55; // Yellow
            _pallete[15] = 0xFFFFFFFF; //White

            _font = fontToUse ?? Fonts.Font18;

            Cols = (int)x / _font.Width - 1;
            Rows = (int)y / _font.Height - 1;

            _text = new Cell[Cols * Rows];

            ClearText();

            CursorVisible = true;
            _textHistory = new List<Cell[]>();

            X = Y = 0;
        }

        private int GetIndex(int row, int column) => row * Cols + column;
        
        public void SetCursorPos(int x, int y)
        {
            if (CursorVisible)
            {
                Canvas.DrawFilledRectangle(ForegroundColor, 0 + x * _font.Width,
                    0 + y * _font.Height + _font.Height, 8, 4);
            }
        }

        public void ClearText()
        {
            Canvas.Clear(Color.Black);

            X = Y = 0;

            for(int i = 0; i < _text.Length; i++)
            {
                _text[i].Char = (char)0;
                _text[i].Foreground = (uint)ForegroundColor.ToArgb();
                _text[i].Background = (uint)BackgroundColor.ToArgb();
            }
            Update();
        }

        public void DrawCursor() => SetCursorPos(X, Y);

        private void NextLine()
        {
            Y++;
            X = 0;
            if (Y == Rows)
            {
                Scroll();
                Y--;
            }
        }

        private void Scroll()
        {
            Canvas.Clear(Color.Black);
            Cell[] lineToHistory = new Cell[Cols];
            Array.Copy(_text, 0, lineToHistory, 0, Cols);

            _textHistory.Add(lineToHistory);

            Array.Copy(_text, Cols, _text, 0, (Rows - 1) * Cols);

            int startIdx = (Rows - 1) * Cols;
            for (int i = startIdx; i < startIdx + Cols; i++)
            {
                _text[i].Char = (char)0;
                _text[i].Foreground = (uint)ForegroundColor.ToArgb();
                _text[i].Background = (uint)BackgroundColor.ToArgb();
            }

            _textHistoryIdx = _textHistory.Count;

            Update();
        }

        public void ScrollUp()
        {
            if(_textHistoryIdx > 0)
            {
                ScrollMode = true;

                _textHistoryIdx--;

                Array.Copy(_text, 0, _text, Cols, (Rows - 1) * Cols);

                Cell[] lineFromHistory = _textHistory[_textHistoryIdx];
                Array.Copy(lineFromHistory, 0, _text, 0, Cols);
                Update();
            }
        }

        public void ScrollDown()
        {
            _textHistoryIdx = 0;
            _textHistory.Clear();

            ScrollMode = false;

            ClearText();
            X = Y = 0;
            Update();
        }

        private void DoCarriage() => X = 0;

        private void DoTab() => Write(_SPACE + _SPACE + _SPACE + _SPACE);

        private void DoBack()
        {
            if (X == 0)
            {
                Y--;
                X = Cols - 1;
            }
            else
                X--;
        }

        public void Write(char ch)
        {
            WriteNoUpdate(ch);

            Update();
        }

        public void WriteNoUpdate(char ch)
        {
            int idx = GetIndex(Y, X);
            _text[idx] = new Cell()
            {
                Char = ch,
                Foreground = (uint)ForegroundColor.ToArgb(),
                Background = (uint)BackgroundColor.ToArgb()
            };

            X++;
            if (X == Cols)
                NextLine();
        }

        public void WriteNoUpdate(string text)
        {
            for(int i = 0; i < text.Length; i++) 
            {
                switch (text[i])
                {
                    case _NEW_LINE:
                        NextLine();
                        break;
                    case _CARRIAGE:
                        DoCarriage();
                        break;
                    case _TAB:
                        DoTab();
                        break;
                    case _BACK:
                        DoBack();
                        break;
                    default:
                        WriteNoUpdate(text[i]);
                        break;
                }
            }
        }

        public void Write(string text)
        {
            WriteNoUpdate(text); 
            Update();
        }

        public void Write(object value) => Write(value.ToString());
        public void WriteLine(char chr) => Write(new string(new char[] { chr, _NEW_LINE }));
        public void WriteLine(string text) => Write(text + _NEW_LINE);
        public void WriteLine(object value) => Write(value.ToString() + _NEW_LINE);
    }
#endif
}
