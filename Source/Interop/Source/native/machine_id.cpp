//-----------------------------------------------------------------------
// <copyright file="machine_id.cpp" company="Microsoft">
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

#include <msclr\auto_gcroot.h>

#include "native\machine_id.h"

#using <Microsoft.PSharp.dll> as_friend

class MachineIdWrapper
{
public:
	msclr::auto_gcroot<Microsoft::PSharp::MachineId^> id;
};

MachineId::MachineId()
{
	this->_id = new MachineIdWrapper();
	//this->_id->id = gcnew Microsoft::PSharp::MachineId();
}

MachineId::~MachineId()
{
	delete this->_id;
}
