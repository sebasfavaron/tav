using UnityEngine;
using System.Collections;

public static class NumberUtils {

	public static bool NearlyEquals(this float value1, float value2, float unimportantDifference = 0.0001f)
	{
		if (value1 != value2){
			return Mathf.Abs(value1 - value2) < unimportantDifference;
		}
		return true;
	}

    public static int Mod(int a, int b) {
        return (a % b + b) % b;
    }

    public static float Mod(float a, float b) {
        return (a % b + b) % b;
    }
}