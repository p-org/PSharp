//-----------------------------------------------------------------------
// <copyright file="Machine.cpp" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

#include "stdafx.h"
#include <msclr\auto_gcroot.h>
#include <typeinfo>
#include <iostream>

#include "Machine.h"

#using <Microsoft.PSharp.dll> as_friend

class MachineWrapper
{
public:
	msclr::auto_gcroot<Microsoft::PSharp::TriggerMachine^> trigger;
};

Machine::Machine()
{
	this->_machine_wrapper = new MachineWrapper();
	this->_machine_wrapper->trigger = gcnew Microsoft::PSharp::TriggerMachine();

	this->_inbox = new std::queue<Event>();
	this->_isRunning = true;
	this->_isHalted = false;

	std::cout << "New machine created" << std::endl;
}

void Machine::send(Id* id, Event* e)
{
	std::cout << "Sending event " << typeid(*e).name() << std::endl;
}

Machine::~Machine()
{
	delete this->_inbox;
	//delete this->received_event;
	delete this->_machine_wrapper;
}
