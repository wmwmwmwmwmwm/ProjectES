using System;

[Serializable]
public class IntInt
{
	public int Item1;
	public int Item2;

	public IntInt() { }

	public IntInt(int item1, int item2)
	{
		Item1 = item1;
		Item2 = item2;
	}
}

[Serializable]
public class IntFloat
{
	public int Item1;
	public float Item2;

	public IntFloat() { }

	public IntFloat(int item1, float item2)
	{
		Item1 = item1;
		Item2 = item2;
	}
}

[Serializable]
public class StringInt 
{
	public string Item1;
	public int Item2;

	public StringInt() { }

	public StringInt(string item1, int item2)
	{
		Item1 = item1;
		Item2 = item2;
	}
}