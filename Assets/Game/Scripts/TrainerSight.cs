using System;

namespace MeadowQuest
{
    public static class TrainerSight
    {
        public static int Distance(int x, int y, int facingX, int facingY, int targetX, int targetY, int range, Func<int, int, bool> walkable)
        {
            if (Math.Abs(facingX) + Math.Abs(facingY) != 1)
                return 0;
            for (int step = 1; step <= range; step++)
            {
                int nextX = x + facingX * step, nextY = y + facingY * step;
                if (!walkable(nextX, nextY))
                    return 0;
                if (nextX == targetX && nextY == targetY)
                    return step;
            }

            return 0;
        }
    }
}
