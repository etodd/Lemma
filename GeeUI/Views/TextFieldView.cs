
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GeeUI.Structs;
using GeeUI.Managers;

namespace GeeUI.Views
{
    public class TextFieldView : View
    {
        public NinePatch NinePatchDefault;
        public NinePatch NinePatchSelected;

        public SpriteFont TextInputFont;

        public bool MultiLine = true;
        public bool Editable = true;

        private string _text = "";
        public string Text
        {
            get
            {
                return MultiLine ? _text : _text.Replace("\n", "");
            }
            set
            {
                _text = value;
            }
        }

        private int _offsetX;
        private int _offsetY;
        private int _cursorX;
        private int _cursorY;

        private Vector2 _selectionStart = new Vector2(-1);
        private Vector2 _selectionEnd = new Vector2(-1);

        private int _buttonHeldTime;

        //How long to press before repeating
        private int _buttonHeldTimePreRepeat = 2;
        private string _buttonHeldString = "";
        private Keys _buttonHeld = Keys.None;

        int _delimiterTime;
        private const int DelimiterLimit = 25;
        bool _doingDelimiter;

        public string OffsetText
        {
            get
            {
                var ret = "";
                var lines = TextLines;
                var allowedWidth = Width;
                var allowedHeight = Height;
                for (var iY = _offsetY; iY < lines.Length; iY++)
                {
                    var curLine = lines[iY];
                    var curLineRet = "";
                    for (int iX = _offsetX; iX < curLine.Length; iX++)
                    {
                        var lineWidth = (int)TextInputFont.MeasureString(curLineRet + curLine[iX]).X;
                        if (lineWidth >= allowedWidth)
                        {
                            break;
                        }
                        curLineRet += curLine[iX];
                    }
                    var retTest = ret + curLineRet + (iY + 1 != lines.Length ? "\n" : "");
                    var maxHeight = (int)TextInputFont.MeasureString(retTest).Y;
                    ret += curLineRet + (iY + 1 != lines.Length ? "\n" : "");
                    if (maxHeight >= allowedHeight)
                        break;
                }
                return ret;
            }
        }


        public string[] TextLines
        {
            get
            {
                return Text.Split('\n');
            }
            set
            {
                string cur = "";
                for (int i = 0; i < value.Length; i++)
                {
                    cur += value[i];
                    if (i < value.Length - 1)
                        cur += "\n";
                }
                Text = cur;
            }
        }


        public TextFieldView(View rootView, Vector2 position, SpriteFont textFont)
            : base(rootView)
        {
            NinePatchDefault = GeeUI.NinePatchTextFieldDefault;
            NinePatchSelected = GeeUI.NinePatchTextFieldSelected;

            Position = position;
            TextInputFont = textFont;
            NumChildrenAllowed = -1;

            GeeUI.OnKeyPressedHandler += keyPressedHandler;
            GeeUI.OnKeyReleasedHandler += keyReleasedHandler;
        }

        void keyReleasedHandler(string keyReleased, Keys key)
        {
            if (_buttonHeld != key) return;
            _buttonHeldTime = 0;
            _buttonHeld = Keys.None;
            _buttonHeldTimePreRepeat = 0;
            _buttonHeldString = "";
        }


        void keyPressedHandler(string keyPressed, Keys key)
        {
            if (!Selected || !Editable) return;
            if (_buttonHeld != key)
            {
                _buttonHeld = key;
                _buttonHeldTime = 0;
                _buttonHeldTimePreRepeat = 0;
                _buttonHeldString = keyPressed;
            }
            bool ctrlPressed = InputManager.IsKeyPressed(Keys.LeftControl) || InputManager.IsKeyPressed(Keys.RightControl);

            if (ctrlPressed)
            {
                switch (key)
                {
                    case Keys.A:
                        break;

                    case Keys.C:
                        break;

                    case Keys.X:
                        break;

                    case Keys.V:
                        break;

                    case Keys.Back:
                        break;
                }
            }
            else
            {
                switch (key)
                {
                    case Keys.Back:
                        BackSpace();
                        break;
                    case Keys.Left:
                        SelectionArrowKeys();
                        MoveCursorX(-1);
                        break;
                    case Keys.Right:
                        SelectionArrowKeys();
                        MoveCursorX(1);
                        break;
                    case Keys.Up:
                        SelectionArrowKeys();
                        MoveCursorY(-1);
                        break;
                    case Keys.Down:
                        SelectionArrowKeys();
                        MoveCursorY(1);
                        break;
                    case Keys.Enter:
                        AppendTextCursor("\n");
                        break;
                    case Keys.Space:
                        AppendTextCursor(" ");
                        break;
                    default:
                        AppendTextCursor(keyPressed);
                        break;
                }
            }
            ReEvaluateOffset();
        }

        private void SelectionArrowKeys()
        {
            if (_selectionEnd == _selectionStart || _selectionEnd == new Vector2(-1)) return;

            var start = _selectionStart;
            var end = _selectionEnd;
            if (_selectionStart.Y > _selectionEnd.Y || (_selectionStart.Y == _selectionEnd.Y && _selectionStart.X > _selectionEnd.X))
            {
                //Need to swap the variables.
                var store = start;
                start = end;
                end = store;
            }
            _cursorX = (int) end.X;
            _cursorY = (int) end.Y;
            _selectionEnd = _selectionStart = new Vector2(-1);
        }

        private void BackSpace()
        {
            if (_selectionStart == _selectionEnd || _selectionEnd == new Vector2(-1))
            {
                var lines = TextLines;
                var curPos = _cursorX;
                for (var i = 0; i < _cursorY; i++)
                {
                    var lineL = lines[i] + (i < _cursorY ? "\n" : "");
                    curPos += lineL.Length;
                }
                if (curPos > 0)
                {
                    Text = Text.Remove(curPos - 1, 1);
                    _cursorX--;
                }
                if (_cursorX < 0)
                {
                    _cursorX = lines[_cursorY - 1].Length;
                    _cursorY--;
                }
                return;
            }


            var start = _selectionStart;
            var end = _selectionEnd;
            if (_selectionStart.Y > _selectionEnd.Y || (_selectionStart.Y == _selectionEnd.Y && _selectionStart.X > _selectionEnd.X))
            {
                //Need to swap the variables.
                var store = start;
                start = end;
                end = store;
            }
            string before = "";
            string after = "";

            float beforeEndX = start.X;
            float beforeEndY = start.Y;
            float afterStartX = end.X;
            float afterStartY = end.Y;

            for (int y = 0; y <= beforeEndY; y++)
            {
                if (y != 0) before += "\n";
                var xUnder = (y == beforeEndY) ? beforeEndX : TextLines[y].Length;
                for (int x = 0; x < xUnder; x++)
                {
                    before += TextLines[y][x];
                }
            }
            for (var y = afterStartY; y < TextLines.Length; y++)
            {
                if (y != afterStartY) after += "\n";
                for (var x = afterStartX; x < TextLines[(int)y].Length; x++)
                {
                    after += TextLines[(int)y][(int)x];
                }
                afterStartX = 0;
            }
            Text = before;
            //Hacky way of easily finding the co-ordinates for the new cursor position.
            _cursorY = TextLines.Length - 1;
            _cursorX = TextLines[_cursorY].Length;
            Text += after;

            _selectionEnd = _selectionStart = new Vector2(-1);
        }

        private void MoveCursorX(int xMovement)
        {
            string[] lines = TextLines;
            _cursorX += xMovement;
            if (_cursorX < 0)
            {
                int yMinus = _cursorY - 1;
                if (yMinus < 0)
                {
                    _cursorX = 0;
                }
                else
                {
                    string line = lines[yMinus];
                    _cursorX = line.Length;
                    _cursorY = yMinus;
                }
            }
            else if (_cursorX > lines[_cursorY].Length)
            {
                if (_cursorY < lines.Length - 1)
                {
                    _cursorY++;
                    _cursorX = 0;
                }
                else
                {
                    _cursorX = lines[_cursorY].Length;
                }
            }

            ReEvaluateOffset();
        }

        private void MoveCursorY(int yMovement)
        {
            string[] lines = TextLines;
            _cursorY += yMovement;
            if (_cursorY >= lines.Length) _cursorY = lines.Length - 1;
            else if (_cursorY < 0) _cursorY = 0;
            string line = lines[_cursorY];
            if (_cursorX >= line.Length) _cursorX = line.Length;

            ReEvaluateOffset();
        }

        private void ReEvaluateOffset()
        {
            if (_selectionStart == _selectionEnd)
                _selectionEnd = _selectionStart = Vector2.Zero;
            var ret = "";
            var lines = TextLines;
            var allowedWidth = Width;
            var allowedHeight = Height;

            var maxCharX = 0;
            var maxCharY = 0;

            var xDiff = _cursorX - _offsetX;
            var yDiff = _cursorY - _offsetY;

            if (xDiff < 0) _offsetX += xDiff;
            if (yDiff < 0) _offsetY += yDiff;

            for (var iY = _offsetY; iY < lines.Length; iY++)
            {
                var curLine = lines[iY];
                var curLineRet = "";
                for (var iX = _offsetX; iX < curLine.Length; iX++)
                {
                    var lineWidth = (int)TextInputFont.MeasureString(curLineRet + curLine[iX]).X;
                    if (lineWidth >= allowedWidth)
                    {
                        break;
                    }
                    curLineRet += curLine[iX];
                    if (iY == _cursorY)
                        maxCharX++;
                }
                ret += curLineRet + (iY + 1 != lines.Length ? "\n" : "");
                var lineHeight = (int)TextInputFont.MeasureString(ret).Y;
                if (lineHeight >= allowedHeight)
                {
                    break;
                }
                maxCharY++;
            }
            if (maxCharX < xDiff)
                _offsetX += xDiff - maxCharX;
            if (maxCharY < yDiff) _offsetY++;
        }

        private void AppendTextCursor(string text)
        {
            string before = "";
            string after = "";

            float beforeEndX = _cursorX;
            float beforeEndY = _cursorY;
            float afterStartX = _cursorX;
            float afterStartY = _cursorY;

            if (_selectionStart != _selectionEnd && _selectionEnd != new Vector2(-1))
            {
                var start = _selectionStart;
                var end = _selectionEnd;
                if (_selectionStart.Y > _selectionEnd.Y || (_selectionStart.Y == _selectionEnd.Y && _selectionStart.X > _selectionEnd.X))
                {
                    //Need to swap the variables.
                    var store = start;
                    start = end;
                    end = store;
                }

                beforeEndX = start.X;
                beforeEndY = start.Y;
                afterStartX = end.X;
                afterStartY = end.Y;
            }
            for (int y = 0; y <= beforeEndY; y++)
            {
                if (y != 0) before += "\n";
                var xUnder = (y == beforeEndY) ? beforeEndX : TextLines[y].Length;
                for (int x = 0; x < xUnder; x++)
                {
                    before += TextLines[y][x];
                }
            }
            for (var y = afterStartY; y < TextLines.Length; y++)
            {
                if (y != afterStartY) after += "\n";
                for (var x = afterStartX; x < TextLines[(int)y].Length; x++)
                {
                    after += TextLines[(int)y][(int)x];
                }
                afterStartX = 0;
            }
            Text = before + text;
            //Hacky way of easily finding the co-ordinates for the new cursor position.
            _cursorY = TextLines.Length - 1;
            _cursorX = TextLines[_cursorY].Length;
            Text += after;

            _selectionEnd = _selectionStart = new Vector2(-1);
        }

        public void AppendText(string text)
        {
            Text += text;
        }

        public Vector2 GetMouseTextPos(Vector2 pos)
        {
            var lines = TextLines;

            var patch = Selected ? NinePatchSelected : NinePatchDefault;

            var topLeftContentPos = AbsolutePosition + new Vector2(patch.LeftWidth, patch.TopHeight);
            var actualClickPos = pos - topLeftContentPos;

            var ret = new Vector2();

            var actualText = "";

            bool setY = false;
            bool setX = false;
            for (var iY = _offsetY; iY < lines.Length; iY++)
            {
                var textHeight = (int)TextInputFont.MeasureString(actualText + lines[iY]).Y;
                if (textHeight >= actualClickPos.Y)
                {
                    ret.Y = iY;
                    setY = true;

                    var line = lines[iY];

                    //No need to make another variable
                    actualText = "";

                    for (int iX = _offsetX; iX < line.Length; iX++)
                    {
                        actualText += line[iX];
                        var textWidth = (int)TextInputFont.MeasureString(actualText).X;
                        if (textWidth < actualClickPos.X) continue;
                        ret.X = iX;
                        setX = true;
                        break;
                    }

                    

                    break;
                }
                actualText += lines[iY] + "\n";
            }
            if (!setY) ret.Y = TextLines.Length - 1;
            if (!setX)
                ret.X = TextLines[(int)ret.Y].Length;
            return ret;
        }

        public override void OnMClick(Vector2 mousePosition, bool fromChild = false)
        {
            Selected = true;

            var clickPos = GetMouseTextPos(mousePosition);
            _cursorX = (int)clickPos.X;
            _cursorY = (int)clickPos.Y;

            _selectionStart = clickPos;

            base.OnMClick(mousePosition);
        }

        public override void OnMClickAway(bool fromChild = false)
        {
            Selected = false;
            _selectionEnd = _selectionStart = new Vector2(-1);
        }

        public override void OnMOver(bool fromChild = false)
        {
            if (Selected && InputManager.IsMousePressed(MouseButton.Left))
            {
                var clickPos = GetMouseTextPos(InputManager.GetMousePosV());
                _selectionEnd = clickPos;
                _cursorX = (int) clickPos.X;
                _cursorY = (int) clickPos.Y;
            }
            base.OnMOver();
        }
        public override void OnMOff(bool fromChild = false)
        {
            if (Selected && InputManager.IsMousePressed(MouseButton.Left))
            {
                
            }
            base.OnMOff();
        }

        private Vector2 GetDrawPosForCursorPos(int cursorX, int cursorY)
        {
            var patch = Selected ? NinePatchSelected : NinePatchDefault;
            var lines = TextLines;

            var totalLine = "";
            for (int i = _offsetY; i < cursorY && i < lines.Length; i++)
            {
                var line = lines[i];
                var addNewline = (i < cursorY - 1) || (i == cursorY && line.Length == 0);
                var addSpace = (line.Length == 0);
                line += (addNewline ? "\n" : "");
                line += (addSpace ? " " : "");
                totalLine += line;
            }

            var yDrawPos = (int)(AbsoluteY + patch.TopHeight + TextInputFont.MeasureString(totalLine).Y);
            var yDrawLine = lines[cursorY];
            var cur = "";
            for (var x = _offsetX; x < cursorX && x < yDrawLine.Length; x++)
                cur += yDrawLine[x];
            var xDrawPos = (int)TextInputFont.MeasureString(cur).X + (AbsoluteX + patch.LeftWidth);

            return new Vector2(xDrawPos, yDrawPos);
        }

        public override void Update(GameTime theTime)
        {
            if (InputManager.IsKeyPressed(_buttonHeld))
            {
                if (!(_buttonHeldTimePreRepeat++ < 15 || _buttonHeldTime++ < 2))
                {
                    _buttonHeldTime = 0;
                    keyPressedHandler(_buttonHeldString, _buttonHeld);
                }
            }
            else
            {
                keyReleasedHandler("", _buttonHeld);
            }
            base.Update(theTime);
        }


        public override void Draw(SpriteBatch spriteBatch)
        {
            var drawPos = GetDrawPosForCursorPos(_cursorX, _cursorY);
            var xDrawPos = drawPos.X;
            var yDrawPos = drawPos.Y;

            var patch = Selected ? NinePatchSelected : NinePatchDefault;
            patch.Draw(spriteBatch, AbsolutePosition, Width, Height);

            if (_selectionStart != _selectionEnd && _selectionEnd != new Vector2(-1))
            {
                var start = _selectionStart;
                var end = _selectionEnd;
                if (_selectionStart.Y > _selectionEnd.Y || (_selectionStart.Y == _selectionEnd.Y && _selectionStart.X > _selectionEnd.X))
                {
                    //Need to swap the variables.
                    var store = start;
                    start = end;
                    end = store;
                }

                for (int y = (int)start.Y; y <= end.Y; y++)
                {
                    string line = TextLines[y];
                    if (line == "") line = " ";
                    var startDrawX = patch.LeftWidth;
                    var startDrawY = GetDrawPosForCursorPos(0, y).Y;
                    var endDrawX = Width - patch.RightWidth;
                    var endDrawY = TextInputFont.MeasureString(line).Y + startDrawY - 1;
                    startDrawX += AbsoluteX;
                    endDrawX += AbsoluteX;

                    if (y == start.Y)
                    {
                        startDrawX = (int)GetDrawPosForCursorPos((int)start.X, (int)start.Y).X;
                    }
                    if (y == end.Y)
                    {
                        endDrawX = (int)GetDrawPosForCursorPos((int)end.X, (int)end.Y).X;
                    }
                    if (y == 0) endDrawY += 1;

                    DrawManager.DrawBox(new Vector2(startDrawX, startDrawY), new Vector2(endDrawX, endDrawY), Color.Blue, spriteBatch, 0f, 20);
                }
            }

            spriteBatch.DrawString(TextInputFont, OffsetText, AbsolutePosition + new Vector2(patch.LeftWidth, patch.TopHeight), Color.Black);

            if (_delimiterTime++ % DelimiterLimit == 0)
            {
                _doingDelimiter = !_doingDelimiter;
            }
            if (_doingDelimiter && Selected && _selectionEnd == _selectionStart)
                spriteBatch.DrawString(TextInputFont, "|", new Vector2(xDrawPos - 1, yDrawPos), Color.Black);
            base.Draw(spriteBatch);
        }
    }
}
