// Benchmarks.h

#pragma once

#using <Microsoft.PSharp.dll>

using namespace System;

namespace Benchmarks {

	public ref class Driver abstract sealed
	{
	public:
		[Microsoft::PSharp::Test]
		static void run();
	};
}
