using GeeUI.Views;

namespace GeeUI.Structs
{
    public static class ViewDepthComparer
    {
        public static int CompareDepths(View view1, View view2)
        {
            if (view2.ThisDepth == -1) return -1;
            return view2.ThisDepth - view1.ThisDepth;
        }

        public static int CompareDepthsInverse(View view1, View view2)
        {
            return view1.ThisDepth - view2.ThisDepth;
        }
    }
}
