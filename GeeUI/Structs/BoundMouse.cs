using System;
using GeeUI.Managers;
namespace GeeUI.Structs
{
    public class CodeBoundMouse
    {
        public Action Lambda;
        public MouseButton BoundMouseButton;
        public bool Press;
        public bool Constant;


        public CodeBoundMouse(Action a, MouseButton button, bool pressing = true, bool constant = false)
        {
            Lambda = a;
            BoundMouseButton = button;
            Press = pressing;
            Constant = (pressing) && constant;
        }
    }
}
