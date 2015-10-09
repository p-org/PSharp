//-----------------------------------------------------------------------
// <copyright file="PSharpRuntime.h" company="Microsoft">
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

#pragma once

#include <memory>
#include <iostream>

#include "psharp.h"

static class __declspec(dllexport) PSharpRuntime final
{
private:


public:
	//template<typename T>
	//static Id* create_machine();

	template<typename T>
	static Id* create_machine()
	{
		auto machine = std::make_shared<T>();
		if (!dynamic_cast<Machine*>(machine.get()))
		{
			throw std::runtime_error("Cannot create a non-machine type.");
		}

		auto id = std::make_shared<Id>();
		return id.get();
	}
};