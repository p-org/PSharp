#include "stdafx.h"
#include <memory>
#include <iostream>
#include "Test.h"

#include "psharp.h"
#include "Events.h"
#include "Server.h"

void Test::doit()
{
	std::cout << "PingPong started." << std::endl;

	auto server = PSharpRuntime::create_machine<Server>();

	std::cout << "PingPong ended." << std::endl;
}