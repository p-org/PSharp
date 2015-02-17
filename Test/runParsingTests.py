#!/usr/bin/env python

import argparse
import os
import subprocess
import sys

class Options:
    ToolPath = ""
    Solution = ""

def crunchTests():
    Options.ToolPath = os.path.join('..', 'Binaries', 'Release', 'PSharpCompiler.exe')
    Options.Solution = os.path.join('Parsing', 'ParsingTest.sln')
    
    print("Running test solution 1 (out of 1)")
    tool = subprocess.Popen([Options.ToolPath, "/s:" + Options.Solution, '/noanalysis'],
        stdout=subprocess.PIPE, shell=True)
    out, err = tool.communicate()

    lines = out.split('\n')
    if lines[len(lines) - 2].strip() == ". Done":
        print("No errors.")
    else:
        print(out)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Script for running the P# parsing tests.')
    args = vars(parser.parse_args())

    crunchTests()

    print("Done.")

    sys.exit(0)
