using System;
using System.Collections.Generic;

namespace GeeUI.Structs
{
    public class DropDownItem
    {
        public List<DropDownItem> Children = new List<DropDownItem>();
        public bool HasChildren;
        public Action OnClick;
        public string Text = "";

        public DropDownItem(string text, Action onClick = null)
        {
            Text = text;
            OnClick = onClick;
        }

        public void AddChild(DropDownItem d)
        {
            Children.Add(d);
            HasChildren = true;
        }

    }
}
