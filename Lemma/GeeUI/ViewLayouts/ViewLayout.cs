using GeeUI.Views;
using System.Collections.Generic;
namespace GeeUI.ViewLayouts
{
    public class ViewLayout
    {
        /// <summary>
        /// Any child in this List will be ignored by the ViewLayout.
        /// </summary>
        public List<View> ExcludedChildren = new List<View>();

        public ViewLayout()
        {

        }

        public virtual void OrderChildren(View parentView)
        {

        }
    }
}
