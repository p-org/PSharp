#include <iostream>

#include "DataNode.h"

DataNode::DataNode(int idx)
{
	this->idx = idx;
	this->log = new Log();
	this->log->value1 = 0;
	this->log->value2 = false;
}

DataNode::~DataNode()
{
	delete this->log;
}

Log* DataNode::create_log()
{
	this->log->value1 = this->log->value1 + 1;
	this->log->value2 = true;

	std::cout << "DataNode " << this->idx << " creates new log " << this->log->value1 << ".\n" << std::endl;

	return this->log;
}
