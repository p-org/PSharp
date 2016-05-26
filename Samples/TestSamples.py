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

def testSamples():
    Options.ToolPath = os.path.join('..', 'Binaries', 'PSharpTester.exe')
    Options.psharpAsLibraryPath = os.path.join('.', 'PsharpAsLibrary', 'Binaries', 'Debug')
    Options.psharpAsLanguagePath = os.path.join('.', 'PsharpAsLanguage', 'Binaries', 'Debug')

    print(". Testing process 1 (out of 2) - testing 'PSharpAsLibrary' samples")
    for sample in Samples.psharpAsLibrary:
        print("... Testing '" + sample + "'")
        tool = subprocess.Popen([Options.ToolPath, "/test:" + Options.psharpAsLibraryPath + os.path.sep + sample + ".dll",
                                 "/i:3"],
            stdout=subprocess.PIPE, shell=True)
        out, err = tool.communicate()
        print(out)
        if tool.returncode != 0:
            print("\nError: Failed to test '" + sample + "'\n")
            sys.exit(1)

    print(". Testing process 2 (out of 2) - testing 'PSharpAsLanguage' samples")
    for sample in Samples.psharpAsLanguage:
        print("... Testing '" + sample + "'")
        tool = subprocess.Popen([Options.ToolPath, "/test:" + Options.psharpAsLanguagePath + os.path.sep + sample + ".dll",
                                 "/i:3"],
            stdout=subprocess.PIPE, shell=True)
        out, err = tool.communicate()
        print(out)
        if tool.returncode != 0:
            print("\nError: Failed to test '" + sample + "'\n")
            sys.exit(1)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Script for testing the P# samples.')
    args = vars(parser.parse_args())

    testSamples()

    print(". Done")

    sys.exit(0)
