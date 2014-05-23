using System;
using Microsoft.Xna.Framework.Input;
namespace GeeUI.Structs
{
    public class CodeBoundKey
    {
        public Action Lambda;
        public Keys BoundKey;
        public bool Constant;
        public bool Press;

        public CodeBoundKey(Action a, Keys key, bool constant = false, bool pressing = true)
        {
            Lambda = a;
            BoundKey = key;
            Press = pressing;
            Constant = pressing && constant;
        }
    }
}
