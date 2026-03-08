using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Numerics;

/*
    Johny has a lot of time on his hands.
    He decides to find the largest positive integer less than 10,000 that is the
    sum of 5 consecutive positive primes.
    Johny also wants the number to be
    divisible by 3. What is this
    integer Johny is trying to
    find?
*/
[module: SkipLocalsInit]
class Program
{
    public const ulong limit = 10000;
    public const ulong totalPrimes = 5;
    public const ulong divisiblity = 3;
    public static nuint thres = 0;
    public static nuint inverses = 0;
    public static nuint InverseFree = 0;
    public static nuint ThresFree = 0;
    public static readonly ulong[] wheelGaps = { 0, 4, 6, 10, 12, 16, 22, 24 };
    public static readonly ulong sqrtLimit = (ulong)Math.Sqrt(limit);

    public static readonly nuint countNeeded = (nuint)((sqrtLimit / 30 + 1) * 8);
    public static nuint count4 = 0;
    public static nuint count8 = 0;
    public static bool brute = false;
    public static bool warmUp = false;

    public async static Task Main(string[] args)
    {
        Stopwatch timer = new Stopwatch();
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        unsafe
        {
            InverseFree = (nuint)NativeMemory.AllocZeroed(countNeeded * sizeof(ulong));
            ThresFree = (nuint)NativeMemory.AllocZeroed(countNeeded * sizeof(ulong));
            thres = ThresFree;
            inverses = InverseFree;
            Console.WriteLine("ok ok");

            count4 = (countNeeded + 3u) & ~3u;
            count8 = (countNeeded + 7u) & ~7u;
            if (args.Length > 0)
            {
                if (args.Contains("--brute")) brute = true;
                if(args.Contains("--warmup")) warmUp = true;
            }

            ulong* invWalk = (ulong*)inverses;
            ulong* thrWalk = (ulong*)thres;
            for (ulong i = 7; i * i <= limit; i += 30)
            {
                int j = 0;
                for (; j < wheelGaps.Length; j++)
                {
                    ulong divisor = i + wheelGaps[j];
                    *invWalk++ = InverseComp(divisor);
                    *thrWalk++ = ThresComp(divisor);
                }
            }
        }
        Console.WriteLine("nice so far");
        double length = 0;
        ulong ticks = 0;
        ulong total = 0;
        ulong[] primes = new ulong[totalPrimes];
        ulong upTo = (limit / totalPrimes);
        ulong mid = totalPrimes / 2;
        Task.Run(() => IsPrime(7)).Wait();
        Console.WriteLine("great great");
        if (warmUp)
        {
            for (int ii = 0; ii < 100001; ii++)
            {
                UpPrime();
                LowPrime();
            }
        }
        else
        {
            for (int ii = 0; ii < 11; ii++)
            {
                UpPrime();
                LowPrime();
            }
        }
        try
        {
            Console.WriteLine("survived");
            timer.Start();
            total = 0;
            ulong i = upTo;
            if (brute)
            {
                Parallel.For(0, int.MaxValue, (index, ParallelLoopState) =>
                {
                    ulong candidate = (ulong)(upTo - (ulong)index);
                    if (IsPrime(candidate))
                    {
                        if (candidate > primes[mid])
                        {
                            primes[mid] = candidate;
                            ParallelLoopState.Stop();
                        }
                    }
                });
            }
            else
            {
                while (true)
                {
                    if (IsPrime(i))
                    {
                        primes[mid] = i;
                        break;
                    }
                    i--;
                }
            }
            if (brute)
            {
                Task findUpperPrimes = Task.Run(() =>
                {
                    UpPrime();
                });
                Task findLowerPrimes = Task.Run(() =>
                {
                    LowPrime();
                });
                await Task.WhenAll(findUpperPrimes, findLowerPrimes);
            }
            else
            {
                UpPrime();
                LowPrime();
            }
            foreach (ulong prime in primes)
            {
                total += prime;
            }
            ulong top = totalPrimes - 1;
            ulong currLeft = primes[0];
            while (total > limit || total % divisiblity != 0)
            {
                ulong right = primes[top];
                ulong nextLeft = currLeft - 1;
                while (!IsPrime(nextLeft)) nextLeft--;
                currLeft = nextLeft;
                total -= primes[top];
                primes[top] = nextLeft;
                total += nextLeft;
                if (top < 1) top = totalPrimes - 1;
                else top--;
            }
            timer.Stop();
            length = timer.ElapsedMilliseconds;
            ticks = (ulong)timer.ElapsedTicks;
        }
        finally
        {
            unsafe
            {
                NativeMemory.Free((void*)InverseFree);
                NativeMemory.Free((void*)ThresFree);
            }
        }

        await WriteLineC($"\nJohny has a lot of time on his hands. \n", 5, 500);
        await WriteLineC($"He decides to find the largest positive integer less \nthan {limit:N0} that is the ", 5);
        await WriteLineC($"sum of {totalPrimes} consecutive positive primes. \n", 5, 500);
        await WriteLineC($"Johny also wants the number to be ", 5);
        await WriteLineC($"divisible by {divisiblity}.\n", 5, 500);
        await WriteLineC($"What is this integer Johny is trying to ", 5);
        await WriteLineC($"find?\n", 5, 500);

        Console.WriteLine();
        await ShowTimer();

        Console.Write("\r");
        await WriteLineC(total > 0 ? $"Johny's special number is {total:N0}!\n" : "Johny boy says no such number exists!\n", 5, 500);
        await WriteLineC($"This took Johny Boy {length} milliseconds! ", 5, 250);
        await WriteLineC($"In other words, {ticks:N0} ticks.\n", 5, 250);
        await WriteLineC($"Were you able to beat Johny?", 5);
        void UpPrime()
        {
            ulong primeTick = 0;
            ulong currentPrime = primes[mid];
            currentPrime++;
            do
            {
                if (IsPrime(currentPrime))
                {
                    primeTick++;
                    primes[mid + primeTick] = currentPrime;
                }
                currentPrime++;
            } while (primeTick < (totalPrimes - 1 - mid));
        }
        void LowPrime()
        {
            if (primes[mid] != 0)
            {
                ulong primeTick = 0;
                ulong currentPrime = primes[mid];
                currentPrime--;
                do
                {
                    if (IsPrime(currentPrime))
                    {
                        primeTick++;
                        primes[mid - primeTick] = currentPrime;
                    }
                    currentPrime--;
                } while (primeTick < mid);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public unsafe static bool IsPrime(ulong prime)
    {
        if (prime == 2 || prime == 3 || prime == 5) return true;
        if (prime % 2 == 0 || prime % 3 == 0 || prime % 5 == 0) return false;

        if (Avx512F.IsSupported)
        {
            Vector512<ulong> vPrime = Vector512.Create(prime);
            nuint j = 0;
            for (; j < count8; j += 8)
            {
                Vector512<ulong> vInv = Vector512.Load((ulong*)inverses + j);
                Vector512<ulong> vThr = Vector512.Load((ulong*)thres + j);

                Vector512<ulong> vRes = Avx512DQ.MultiplyLow(Vector512.AsInt64(vPrime), Vector512.AsInt64(vInv)).AsUInt64();

                var vCom = Avx512F.CompareLessThanOrEqual(vRes, vThr);

                //if (!vCom.Equals(Vector512<ulong>.Zero)) return false;

                if (Vector512.EqualsAny(vCom, Vector512<ulong>.Zero.WithElement(0, 0xFFFFFFFFFFFFFFFFu)))
                {
                    if (vCom != Vector512<ulong>.Zero) return false;
                }

            }
        }
        else
        {
            ulong i = 7;
            for (; i * i <= prime; i += 30)
            {
                if (prime % i == 0) return false;
                if (prime % (i + 4) == 0) return false;
                if (prime % (i + 6) == 0) return false;
                if (prime % (i + 10) == 0) return false;
                if (prime % (i + 12) == 0) return false;
                if (prime % (i + 16) == 0) return false;
                if (prime % (i + 22) == 0) return false;
                if (prime % (i + 24) == 0) return false;
            }
        }
        return true;
    }
    public static ulong InverseComp(ulong d)
    {
        ulong m = d;
        m *= 2 - d * m;
        m *= 2 - d * m;
        m *= 2 - d * m;
        m *= 2 - d * m;
        m *= 2 - d * m;
        m *= 2 - d * m;
        return m;
    }
    public static ulong ThresComp(ulong d)
    {
        return ulong.MaxValue / d;
    }
    public static async Task ShowTimer()
    {
        float timerStart = 10.00f;
        for (float i = timerStart; i > 0; i -= 0.01f)
        {
            Console.Write($"\rTime remaining: {i:N2}");
            await Task.Delay(1);
        }
        Console.Write("\r                                                                            ");
    }
    public static async Task WriteLineC(string chars)
    {
        char[] stream = chars.ToCharArray();
        foreach (char c in stream)
        {
            Console.Write(c);
            await Task.Delay(1);
        }
    }
    public static async Task WriteLineC(string chars, int delay)
    {
        char[] stream = chars.ToCharArray();
        foreach (char c in stream)
        {
            Console.Write(c);
            await Task.Delay(delay);
        }
    }
    public static async Task WriteLineC(string chars, int delay, int endDelay)
    {
        char[] stream = chars.ToCharArray();
        foreach (char c in stream)
        {
            Console.Write(c);
            await Task.Delay(delay);
        }
        await Task.Delay(endDelay);
    }
}
