using System.Diagnostics;

namespace SqlPad
{
	[DebuggerDisplay("SourcePosition (IndexStart={IndexStart}, IndexEnd={IndexEnd}, Length={Length})")]
	public struct SourcePosition
	{
		public static SourcePosition Empty = new SourcePosition { IndexStart = -1, IndexEnd = -1 };

		public static SourcePosition Create(int indexStart, int indexEnd)
		{
			return
				new SourcePosition
				{
					IndexStart = indexStart,
					IndexEnd = indexEnd
				};
		}

		public int IndexStart { get; private set; }

		public int IndexEnd { get; private set; }

		public int Length => IndexEnd - IndexStart + 1;

		public bool ContainsIndex(int index, bool acceptNextCharacter = true)
		{
			return IndexStart <= index && index <= IndexEnd + (acceptNextCharacter ? 1 : 0);
		}

		public bool Contains(SourcePosition sourcePosition)
		{
			return IndexStart <= sourcePosition.IndexStart && IndexEnd >= sourcePosition.IndexEnd;
		}

		public bool Equals(SourcePosition other)
		{
			return IndexStart == other.IndexStart && IndexEnd == other.IndexEnd;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is SourcePosition && Equals((SourcePosition)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (IndexStart * 397) ^ IndexEnd;
			}
		}

		public static bool operator ==(SourcePosition left, SourcePosition right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SourcePosition left, SourcePosition right)
		{
			return !left.Equals(right);
		}
	}
}