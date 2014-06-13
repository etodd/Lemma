using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lemma.Util
{
	public static class BitWorker
	{
		public static bool BitSet(this byte toCheck, int index)
		{
			return ((toCheck & ((byte)1 << index)) != 0);
		}

		public static bool BitSet(this short toCheck, int index)
		{
			return ((toCheck & ((short)1 << index)) != 0);
		}

		public static bool BitSet(this int toCheck, int index)
		{
			return ((toCheck & ((int)1 << index)) != 0);
		}

		public static bool BitSet(this long toCheck, int index)
		{
			return ((toCheck & ((long)1 << index)) != 0);
		}

		public static byte SetBit(this byte toSet, int index, bool set = true)
		{
			if (set)
			{
				return (byte)(toSet | ~(1 << index));
			}
			else
			{
				return (byte)(toSet & ~(1 << index));
			}
		}

		public static short SetBit(this short toSet, int index, bool set = true)
		{
			if (set)
			{
				return (short)(toSet | ~(1 << index));
			}
			else
			{
				return (short)(toSet & ~(1 << index));
			}
		}

		public static int SetBit(this int toSet, int index, bool set = true)
		{
			if (set)
			{
				return (toSet | (1 << index));
			}
			else
			{
				return (toSet & ~(1 << index));
			}
		}

		public static long SetBit(this long toSet, int index, bool set = true)
		{
			if (set)
			{
				return (toSet | (1 << index));
			}
			else
			{
				return (toSet & ~(1 << index));
			}
		}

		public static short StoreByte(this short toSet, byte toStore, int index = 0)
		{
			if (index > 7) throw new ArgumentException("Index to store byte must not be over 7");
			for (int i = index; i < index + 8; i++)
				toSet = toSet.SetBit(i, toStore.BitSet(i - index));
			return toSet;
		}

		public static int StoreByte(this int toSet, byte toStore, int index = 0)
		{
			if (index > 24) throw new ArgumentException("Index to store byte must not be over 24");
			for (int i = index; i < index + 8; i++)
				toSet = toSet.SetBit(i, toStore.BitSet(i - index));
			return toSet;
		}

		public static int StoreShort(this int toSet, short toStore, int index = 0)
		{
			if (index > 16) throw new ArgumentException("Index to store byte must not be over 16");
			for (int i = index; i < index + 16; i++)
				toSet = toSet.SetBit(i, toStore.BitSet(i - index));
			return toSet;
		}

		public static long StoreByte(this long toSet, byte toStore, int index = 0)
		{
			if (index > 55) throw new ArgumentException("Index to store byte must not be over 55");
			for (int i = index; i < index + 8; i++)
				toSet = toSet.SetBit(i, toStore.BitSet(i - index));
			return toSet;
		}

		public static long StoreShort(this long toSet, short toStore, int index = 0)
		{
			if (index > 47) throw new ArgumentException("Index to store byte must not be over 47");
			for (int i = index; i < index + 16; i++)
				toSet = toSet.SetBit(i, toStore.BitSet(i - index));
			return toSet;
		}

		public static long StoreInt(this long toSet, int toStore, int index = 0)
		{
			if (index > 31) throw new ArgumentException("Index to store byte must not be over 31");
			for (int i = index; i < index + 32; i++)
				toSet = toSet.SetBit(i, toStore.BitSet(i - index));
			return toSet;
		}

		public static byte ExtractBits(this byte toExtract, int index, int num)
		{
			byte ret = 0;
			for (int i = index; i < index + num; i++)
			{
				ret = ret.SetBit(i - index, toExtract.BitSet(i));
			}
			return ret;
		}

		public static short ExtractBits(this short toExtract, int index, int num)
		{
			short ret = 0;
			for (int i = index; i < index + num; i++)
			{
				ret = ret.SetBit(i - index, toExtract.BitSet(i));
			}
			return ret;
		}

		public static int ExtractBits(this int toExtract, int index, int num)
		{
			int ret = 0;
			for (int i = index; i < index + num; i++)
			{
				ret = ret.SetBit(i - index, toExtract.BitSet(i));
			}
			return ret;
		}

		public static long ExtractBits(this long toExtract, int index, int num)
		{
			long ret = 0;
			for (int i = index; i < index + num; i++)
			{
				ret = ret.SetBit(i - index, toExtract.BitSet(i));
			}
			return ret;
		}

		public static int[] PackInts(int numBitsPer, params int[] ints)
		{
			int numBitsPerInt = 32;
			if (numBitsPer > numBitsPerInt) throw new ArgumentException("You are an idiot.");
			List<int> ret = new List<int>();
			int currentInt = 0;
			int currentIntIndex = 0;
			int storingIntIndex = 0;
			int storingInt = 0;
			int i;

			for (i = 0; i < ints.Length; i++)
			{
				int theInt = ints[i];
				if (theInt < 0)
				{
					ints[i] = (-theInt).SetBit(numBitsPer - 1, true);
				}
				else
				{
					ints[i] = theInt.SetBit(numBitsPer - 1, false);
				}
			}
			i = 0;
			
			while (true)
			{
				if (i >= ints.Length)
				{
					if (currentIntIndex != 0)
						ret.Add(currentInt);
					break;
				}
				storingInt = ints[i];
				
				bool incrementI = (numBitsPer - storingIntIndex) <= (numBitsPerInt - currentIntIndex);
				for (; storingIntIndex < numBitsPer; storingIntIndex++)
				{
					//We will NOT exhaust one of our ints for storing. That is, we will fully store this int.
					if (incrementI)
					{
						currentInt = currentInt.SetBit(currentIntIndex++, storingInt.BitSet(storingIntIndex));
					}
					//We WILL NOT fully store this int.
					else
					{
						if (currentIntIndex >= numBitsPerInt)
						{
							ret.Add(currentInt);
							currentIntIndex = 0;
							currentInt = 0;
							incrementI = true;
						}
						currentInt = currentInt.SetBit(currentIntIndex++, storingInt.BitSet(storingIntIndex));
					}
				}
				if (currentIntIndex >= numBitsPerInt)
				{
					ret.Add(currentInt);
					currentIntIndex = 0;
					currentInt = 0;
					incrementI = true;
				}
				if (incrementI)
				{
					i++;
					storingIntIndex = 0;
				}
			}

			return ret.ToArray();
		}

		public static int[] UnPackInts(int numBitsPer, int numToPull = -1, params int[] ints)
		{
			int numBitsPerInt = 32;
			if (numBitsPer > numBitsPerInt) throw new ArgumentException("You are an idiot.");

			if (numToPull == -1)
			{
				numToPull = (int)Math.Floor((float)(numBitsPerInt * ints.Length) / (float)numBitsPer);
			}

			List<int> ret = new List<int>();
			int currentInt = 0;
			int currentIntIndex = 0;
			int unPackingIntIndex = 0;
			int unPackingInt = 0;
			int i = 0;
			while (true)
			{
				if (i >= ints.Length)
				{
					if (currentIntIndex != 0)
						ret.Add(currentInt);
					break;
				}
				unPackingInt = ints[i];
				for (; unPackingIntIndex < numBitsPerInt; unPackingIntIndex++)
				{
					if (currentIntIndex >= numBitsPer)
					{
						ret.Add(currentInt);
						currentIntIndex = 0;
						currentInt = 0;
					}
					currentInt = currentInt.SetBit(currentIntIndex++, unPackingInt.BitSet(unPackingIntIndex));
				}
				i++;
				unPackingIntIndex = 0;
			}
			for(i = 0; i < ret.Count; i++)
			{
				int b = ret[i];
				if (b.BitSet(numBitsPer - 1))
				{
					b = -(b.SetBit(numBitsPer - 1, false));
					ret[i] = b;
				}
			}
			if (ret.Count > numToPull) return ret.GetRange(0, numToPull).ToArray();
			return ret.ToArray();
		}
	}
}
