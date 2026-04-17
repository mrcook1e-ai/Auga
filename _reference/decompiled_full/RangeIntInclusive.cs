public struct RangeIntInclusive
{
	public readonly int m_minValue;

	public readonly int m_maxValue;

	public static RangeIntInclusive Positive => new RangeIntInclusive(0, int.MaxValue);

	public static RangeIntInclusive Full => new RangeIntInclusive(int.MinValue, int.MaxValue);

	public RangeIntInclusive(int maxValue)
	{
		m_minValue = 0;
		m_maxValue = maxValue;
	}

	public RangeIntInclusive(int minValue, int maxValue)
	{
		m_minValue = minValue;
		m_maxValue = maxValue;
	}
}
