namespace MeadowQuest
{
    public static class CaveLayout
    {
        public static bool IsFloor(int x,int y) =>
            Rect(x,y,18,1,22,28) || Rect(x,y,15,1,23,6) || Rect(x,y,16,7,22,9) || Rect(x,y,13,10,25,17) || Rect(x,y,18,18,24,21) || Rect(x,y,14,22,24,28) || Rect(x,y,5,12,16,14) || Rect(x,y,3,10,9,17) || Rect(x,y,21,23,33,25) || Rect(x,y,30,21,36,27);
        static bool Rect(int x,int y,int l,int b,int r,int t) => x>=l && x<=r && y>=b && y<=t;
    }
}
