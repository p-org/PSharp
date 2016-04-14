#!/usr/bin/env python

import argparse
import os
import subprocess
import sys

class Options:
    ToolPath = ""
    psharpAsLibraryPath = ""
    psharpAsLanguagePath = ""

class Samples(object):
    psharpAsLibrary = ['BoundedAsync', 'ChainReplication', 'Chord', 'FailureDetector', 'German', 'MultiPaxos',
                       'PingPong', 'Raft', 'ReplicatingStorage', 'TwoPhaseCommit', 'LeaderElection', 'PiCompute',
                       'Chameneos', 'Swordfish']
    psharpAsLanguage = ['AbstractPong', 'BoundedAsync', 'FailureDetector', 'German', 'MultiPaxos', 'PartialPingPong',
                        'PingPong', 'SimpleAsyncAwait', 'TypesAndGenerics']

def compileSamples():
    Options.ToolPath = os.path.join('..', 'Binaries', 'Release', 'PSharpCompiler.exe')
    Options.psharpAsLibraryPath = os.path.join('.', 'PsharpAsLibrary', 'Samples.sln')
    Options.psharpAsLanguagePath = os.path.join('.', 'PsharpAsLanguage', 'Samples.sln')

    print(". Compilation process 1 (out of 2) - compiling 'PSharpAsLibrary' samples")
    for sample in Samples.psharpAsLibrary:
        print("... Compiling '" + sample + "' for execution")
        tool = subprocess.Popen([Options.ToolPath, "/s:" + Options.psharpAsLibraryPath, "/p:" + sample],
            stdout=subprocess.PIPE, shell=True)
        out, err = tool.communicate()
        if tool.returncode != 0:
            print("\nError: Failed to compile '" + sample + "' for execution:\n")
            print(out)
            sys.exit(1)

        print("... Compiling '" + sample + "' for testing")
        tool = subprocess.Popen([Options.ToolPath, "/s:" + Options.psharpAsLibraryPath, "/p:" + sample, '/t:test'],
            stdout=subprocess.PIPE, shell=True)
        out, err = tool.communicate()
        if tool.returncode != 0:
            print("\nError: Failed to compile '" + sample + "' for testing:\n")
            print(out)
            sys.exit(1)

    print(". Compilation process 2 (out of 2) - compiling 'PSharpAsLanguage' samples")
    for sample in Samples.psharpAsLanguage:
        print("... Compiling '" + sample + "' for execution")
        tool = subprocess.Popen([Options.ToolPath, "/s:" + Options.psharpAsLanguagePath, "/p:" + sample],
            stdout=subprocess.PIPE, shell=True)
        out, err = tool.communicate()
        if tool.returncode != 0:
            print("\nError: Failed to compile '" + sample + "' for execution:\n")
            print(out)
            sys.exit(1)
        
        print("... Compiling '" + sample + "' for testing")
        tool = subprocess.Popen([Options.ToolPath, "/s:" + Options.psharpAsLanguagePath, "/p:" + sample, '/t:test'],
            stdout=subprocess.PIPE, shell=True)
        out, err = tool.communicate()
        if tool.returncode != 0:
            print("\nError: Failed to compile '" + sample + "' for testing:\n")
            print(out)
            sys.exit(1)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Script for compiling the P# samples.')
    args = vars(parser.parse_args())

    compileSamples()

    print(". Done")

    sys.exit(0)
