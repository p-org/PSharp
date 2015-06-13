#!/usr/bin/env python

import argparse
import os
import subprocess
import sys

tests = [
          # P benchmarks
          'PBoundedAsync',
		  'PElevator',
		  'PGerman',
		  'PPushTest',

		  # P# benchmarks
		  'AbstractPong',
		  'BoundedAsync',
		  'German',
		  'MultiPaxos',
		  'PingPong',
		  'TypesAndGenerics',

		  # P# distributed benchmarks
		  'DistributedPingPong',

		  # Regressions
		  # Feature1SMLevelDecls
		  'AlonBug',
		  'BugRepro1',
		  'MaxInstances_2',
		  'MaxInstances_3',
		  'AlonBug_Fails',
		  'BugRepro',
		  'MaxInstances_1',

		  # Feature2Stmts
		  'SEM_OneMachine_33',
		  'SEM_OneMachine_34',
		  'SEM_OneMachine_35',

		  # Feature3Exprs
		  #'ExprOperatorsAsserts',
		  #'ShortCircuitEval'
        ]

class Options:
    ToolPath = ""
    Solution = ""

def crunchTests():
    Options.ToolPath = os.path.join('..', 'Binaries', 'Release', 'PSharpCompiler.exe')
    Options.Solution = "Test.sln"
	
    for test in tests:
	  print("Compiling benchmark " + test)
	  tool = subprocess.Popen([Options.ToolPath, "/s:" + Options.Solution, "/p:" + test],
        stdout=subprocess.PIPE, shell=True)
	  out, err = tool.communicate()
	  
	  lines = out.split('\n')
	  if lines[len(lines) - 2].strip() == ". Done":
	    print("No errors.")
	  else:
	    print(out)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Script for running the P# compilation tests.')
    args = vars(parser.parse_args())

    crunchTests()

    print("Done.")

    sys.exit(0)
