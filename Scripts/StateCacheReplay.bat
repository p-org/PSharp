set steps=%1
set iters=%2

.\PSharpCompiler.exe /s:D:\PSharp\Samples\Experimental\LivenessBenchmarks\BuggyBenchmarks\BuggyBenchmarks.sln /t:test
.\PSharpCompiler.exe /s:D:\PSharp\Samples\Experimental\LivenessBenchmarks\SpinBenchmarks\SpinBenchmarks.sln /t:test
.\PSharpCompiler.exe /s:D:\vnext\PyraStor\PyraStor\PyraStor.sln /p:NameNode.PSharp /t:test

.\PSharpTester.exe /test:D:\PSharp\Samples\Experimental\LivenessBenchmarks\BuggyBenchmarks\Chord\bin\Debug\Chord.dll /cycle-replay /max-steps:%steps% /i:%iters%
.\PSharpTester.exe /test:D:\PSharp\Samples\Experimental\LivenessBenchmarks\BuggyBenchmarks\ReplicatingStorage\bin\Debug\ReplicatingStorage.dll /cycle-replay /max-steps:%steps% /i:%iters%
.\PSharpTester.exe /test:D:\PSharp\Samples\Experimental\LivenessBenchmarks\BuggyBenchmarks\Raft\bin\Debug\Raft.dll /cycle-replay /max-steps:%steps% /i:%iters%

.\PSharpTester.exe /test:D:\PSharp\Samples\Experimental\LivenessBenchmarks\SpinBenchmarks\SpinBenchmarks\bin\Debug\ProcessScheduler.dll /cycle-replay /max-steps:%steps% /i:%iters%
.\PSharpTester.exe /test:D:\PSharp\Samples\Experimental\LivenessBenchmarks\SpinBenchmarks\LeaderElection\bin\Debug\LeaderElection.dll /cycle-replay /max-steps:%steps% /i:%iters%
.\PSharpTester.exe /test:D:\PSharp\Samples\Experimental\LivenessBenchmarks\SpinBenchmarks\SlidingWindowProtocol\bin\Debug\SlidingWindowProtocol.dll /cycle-replay /max-steps:%steps% /i:%iters%

.\PSharpTester.exe /test:D:\vnext\PyraStor\NameNode.PSharp\bin\Debug\NameNode.PSharp.dll /cycle-replay /max-steps:%steps% /i:%iters%