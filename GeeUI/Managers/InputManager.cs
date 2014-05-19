using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GeeUI.Structs;
namespace GeeUI.Managers
{
    public class InputManager
    {
        private static KeyboardState _keyboardState;
        private static KeyboardState _oldKeyboardState;
        private static MouseState _mouseState;
        private static MouseState _oldMouseState;
        private int _scrollValue;

        private static List<CodeBoundMouse> _boundMouse = new List<CodeBoundMouse>();
        private static List<CodeBoundKey> _boundKey = new List<CodeBoundKey>();

        private static List<CodeBoundMouse> _toBindMouse = new List<CodeBoundMouse>();
        private static List<CodeBoundKey> _toBindKey = new List<CodeBoundKey>();

        public static void BindKey(Action lambda, Keys key, bool constant = false, bool press = true)
        {
            _toBindKey.Add(new CodeBoundKey(lambda, key, constant, press));
        }

        public static void BindMouse(Action a, MouseButton button, bool press = true, bool constant = false)
        {
            _toBindMouse.Add(new CodeBoundMouse(a, button, press, constant));
        }

        public static Point GetMousePos()
        {
            return new Point(_mouseState.X, _mouseState.Y);
        }

        public static Vector2 GetMousePosV()
        {
            return new Vector2(GetMousePos().X, GetMousePos().Y);
        }

        public void Update(GameTime time)
        {
            _keyboardState = Keyboard.GetState();
            _mouseState = Mouse.GetState();
            int scroll = _mouseState.ScrollWheelValue - _scrollValue;
            _scrollValue = _mouseState.ScrollWheelValue;
            if (GeeUI.TheGame.IsActive)
            {
                foreach (CodeBoundMouse b in _boundMouse)
                {
                    switch (b.BoundMouseButton)
                    {
                        case MouseButton.Left:
                            if (_mouseState.LeftButton != _oldMouseState.LeftButton || b.Constant)
                            {
                                if ((b.Press && _mouseState.LeftButton == ButtonState.Pressed) ||
                                    (!b.Press && _mouseState.LeftButton == ButtonState.Released))
                                    b.Lambda();
                            }
                            break;
                        case MouseButton.Middle:
                            if (_mouseState.MiddleButton != _oldMouseState.MiddleButton || b.Constant)
                            {
                                if ((b.Press && _mouseState.MiddleButton == ButtonState.Pressed) ||
                                    (!b.Press && _mouseState.MiddleButton == ButtonState.Released))
                                    b.Lambda();
                            }
                            break;

                        case MouseButton.Right:
                            if (_mouseState.RightButton != _oldMouseState.RightButton || b.Constant)
                            {
                                if ((b.Press && _mouseState.RightButton == ButtonState.Pressed) ||
                                    (!b.Press && _mouseState.RightButton == ButtonState.Released))
                                    b.Lambda();
                            }
                            break;

                        case MouseButton.Scroll:
                            if (scroll != 0)
                                b.Lambda();
                            break;

                        case MouseButton.Scrolldown:
                            if (scroll < 0)
                                b.Lambda();
                            break;
                        case MouseButton.Scrollup:
                            if (scroll > 0)
                                b.Lambda();
                            break;

                        case MouseButton.Movement:
                            if (_mouseState.Y != _oldMouseState.Y || _mouseState.X != _oldMouseState.X)
                            {
                                b.Lambda();
                            }
                            break;
                    }
                }

                foreach (CodeBoundKey b in _boundKey)
                {
                    Keys k = b.BoundKey;
                    bool newP = _keyboardState.IsKeyDown(k);
                    bool oldP = _oldKeyboardState.IsKeyDown(k);

                    if ((newP != oldP || b.Constant) && b.Press == newP)
                    {
                        b.Lambda();
                    }
                }
            }

            foreach (CodeBoundMouse cbm in _toBindMouse)
            {
                _boundMouse.Add(cbm);
            }
            foreach (CodeBoundKey cbk in _toBindKey)
            {
                _boundKey.Add(cbk);
            }

            _toBindKey.Clear();
            _toBindMouse.Clear();

            _oldMouseState = _mouseState;
            _oldKeyboardState = _keyboardState;
        }

        public static bool IsKeyPressed(Keys k)
        {
            return _keyboardState.IsKeyDown(k);
        }

        public static bool IsMousePressed(MouseButton button)
        {
            switch(button)
            {
                case MouseButton.Left:
                    return _mouseState.LeftButton == ButtonState.Pressed;
                case MouseButton.Middle:
                    return _mouseState.MiddleButton == ButtonState.Pressed;
                case MouseButton.Right:
                    return _mouseState.RightButton == ButtonState.Pressed;
                default:
                    return false;
            }
        }
    }
    public enum MouseButton
    {
        Left,
        Middle,
        Right,
        Scrollup,
        Scrolldown,
        Scroll,
        Movement
    }
    public enum TextAlign
    {
        Left,
        Center,
        Right
    }
}
