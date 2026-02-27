
//Responsible for point management
//How points work whenever point reach to min threshold it's stop spawning bird; 
public static class PointManager
{
    public static float totalPoints;

    public static void AddPoint(float point)
    {
        totalPoints += point;
    }

    public static void RemovePoint(float point)
    {
        if (totalPoints > 0)
        {
            totalPoints -= point;
        }
        else
        {
            totalPoints = 0;
        }
    }
}
