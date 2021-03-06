﻿using System;
using System.Collections.Generic;
using Gbd.PeriodMatching.Tools;
using NLog;
using NUnit.Framework;

namespace Gbd.PeriodMatching.Tests
{
  public class RandomSuppliedValuesTester : PeriodMatcherTester
  {


    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    internal Random Rnd = new Random(DateTime.Now.Millisecond);


    #region Helpers


    protected internal List<long> GeneratePeriodsForTimer(long timer, int nbPeriods)
    {
      List<long> periods = new List<long>(nbPeriods);

      for (int i = 0; i < nbPeriods; i++)
      {
        periods.Add(MultiplyByRandomPowerOf2NoOverflow(timer));
      }

      return periods;
    }

    public long MultiplyByRandomPowerOf2NoOverflow(int selectedTimerHOB,int selectedTimerLOB)
    {
      return MultiplyByRandomPowerOf2NoOverflow((uint) selectedTimerHOB, (uint) selectedTimerLOB);
    }
    public long MultiplyByRandomPowerOf2NoOverflow(long selectedTimer)
    {
      SplitLong split = new SplitLong(selectedTimer);
      return MultiplyByRandomPowerOf2NoOverflow(split.HOB, split.LOB);
    }
    public long MultiplyByRandomPowerOf2NoOverflow(SplitLong selectedTimer)
    {
      return MultiplyByRandomPowerOf2NoOverflow(selectedTimer.HOB, selectedTimer.LOB);
    }
    public long MultiplyByRandomPowerOf2NoOverflow(uint selectedTimerHOB,uint selectedTimerLOB)
    {
      long selectedTimer = (((long) selectedTimerHOB) << 32) | selectedTimerLOB;

      //Log.Warn("");
      //Log.Warn("Trying to generate a multiplier for timer value 0x {0:X8} {0:X8} = {0:X16}", selectedTimerHOB, selectedTimerLOB, selectedTimer);

      long maxMultiplierForNotOverflowing = (long.MaxValue/selectedTimer);
      double maxLog2ForNotOverflowing = Math.Log(maxMultiplierForNotOverflowing, 2);
      int maxShiftForNotOverflowing = (int) maxLog2ForNotOverflowing;
      int currentShift = Rnd.Next(maxShiftForNotOverflowing);

      //Log.Warn(String.Format("  - Max multiplier = {0:0} (0x {0:X16} )", maxMultiplierForNotOverflowing));
      //Log.Warn("  - Max Log2 = " + maxLog2ForNotOverflowing);
      //Log.Warn("  - Max Shift = " + maxShiftForNotOverflowing);

      if (currentShift > maxShiftForNotOverflowing)
      {
        Log.Warn(String.Format("  => shift {0:0} is bigger than its max.", currentShift));
        throw new InvalidOperationException("Multiplier generation went wrong (1st check: shift comparison)");
      }

      if ((1 << currentShift) > maxMultiplierForNotOverflowing)
      {
        Log.Warn(String.Format("  => multiplier {0:0} (0x {0:X16}) is bigger than its max (shift {1:0}).", (1 << currentShift), currentShift));
        throw new InvalidOperationException("Multiplier generation went wrong (1st check: shift comparison)");
      }

      long multiplicationResult = selectedTimer << currentShift;
      if (multiplicationResult <= 0)
      {
        Log.Warn(String.Format("  => multiplication result overflows : {0:0} (0x {0:X16}). Shift = {1:0}", multiplicationResult, currentShift));
        Log.Warn(String.Format("    /  {0:X16} << {1:0} = {2:X16}", selectedTimer, currentShift, multiplicationResult));
        throw new InvalidOperationException("Multiplier generation went wrong (2nd check: result comparison)");
      }

      //Log.Warn(String.Format("Multiplier successfully generated - {0:X16} << {1:0} = {2:X16}", selectedTimer, currentShift, multiplicationResult));
      return multiplicationResult;
    }

    #endregion

    [Test]
    public void AssignTesterSimpleNominalCases(
      [Values(444, 879521, 8879543)]      int randomSeed,
      [Values(200, MaxS32 - 600)]         long timer,
      [Values(1, 5, 50)]                  int nbPeriods)
    {
      Rnd = new Random(randomSeed);
      Sandbox.PeriodsToMatch = GeneratePeriodsForTimer(timer, nbPeriods);

      Sandbox.Assign();

      Assert.That(Sandbox.TimersAssignment.Count, Is.EqualTo(1));
      foreach (long period in Sandbox.TimersAssignment)
      {
        Assert.That(PowerOfTwoMath.IsAPowerOfTwoProduct(period, timer));
      }
    }


    [Test]
    public void AssignTesterNominalCasesWithSeveralTimersNeeded(
      int randomSeed,
      int maxSimilarPeriods, 
      int nbPeriods)
    {
      Random rnd = new Random(randomSeed);
      List<long> periods = new List<long>(nbPeriods);

      for (int i = 0; i < nbPeriods; i++ )
      {
        int nbSimilar = rnd.Next(Math.Min(maxSimilarPeriods, nbPeriods-i));
        SplitLong refTimer = new SplitLong(Rnd.Next());
        //periods.AddRange(GeneratePeriodsForTimer());
      }


      periods.Shuffle(rnd);
      Sandbox.PeriodsToMatch = periods;
      Sandbox.Assign();

      throw new NotImplementedException();
    }
  }
}
