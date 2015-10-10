//-----------------------------------------------------------------------
// <copyright file="psharp_runtime.h" company="Microsoft">
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

#using <Microsoft.PSharp.dll> as_friend
#using <Microsoft.PSharp.BugFindingRuntime.dll> as_friend

namespace Microsoft
{
	namespace PSharp
	{
		namespace Interop
		{
			public ref class Runtime abstract sealed
			{
			public:
				static void send(Microsoft::PSharp::Id^ mid, Microsoft::PSharp::Event^ e)
				{
					Microsoft::PSharp::PSharpRuntime::Send(mid, e);
				}

				static bool non_deterministic_choice()
				{
					return Microsoft::PSharp::PSharpRuntime::Nondet();
				}
			};
		}
	}
}