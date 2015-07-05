#!/usr/bin/env python

import argparse
import os
import subprocess
import sys

tests = [
          # P benchmarks
          #'PBoundedAsync',
		  #'PElevator',
		  #'PGerman',
		  #'PPushTest',

		  # P# benchmarks
		  'AbstractPong',
		  'BoundedAsync',
		  'German',
		  'MultiPaxos',
		  'FailureDetector',
		  'PingPong',
		  'TypesAndGenerics',
        ]

class Options:
    ToolPath = ""
    Solution = ""

def crunchTests():
    Options.ToolPath = os.path.join('..', 'Binaries', 'Release', 'PSharpCompiler.exe')
    Options.Solution = "PSharp.sln"
	
    for test in tests:
	  print("Compiling benchmark " + test)
	  tool = subprocess.Popen([Options.ToolPath, "/s:" + Options.Solution, "/p:" + test, "/test", "/db:1"],
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
