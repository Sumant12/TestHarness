﻿/////////////////////////////////////////////////////////////////////
// AnotherTested.cs - code to test                                 //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
  public class AnotherTested
  {
    public bool myWackyFunction()
    {
      return false;
    }
#if (TEST_TESTED)
    static void Main(string[] args)
    {
    }
#endif
  }
}
