// Loader.WIN32.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//

#include "pch.h"
#include <iostream>
#include <fstream>
#include <Windows.h>
#include "../VM/VM.h"
#include "HostCalls.hpp"

static void BindHostCall(VM::Engine& engine);

int main()
{
	int result = 32;
	std::fstream in;
	std::wcout.imbue(std::locale(""));
	wchar_t moduleName[MAX_PATH] = { 0 };
	GetModuleFileNameW(NULL, moduleName, MAX_PATH);

	in.open(moduleName, std::ios::binary | std::ios::in);
	if (in.good())
	{
		uint64_t offset = 0;
		in.seekg(0, std::ios::end);
		in.seekg(-static_cast<int64_t>(sizeof(offset)), std::ios::cur);
		in.read((char*)(&offset), sizeof(offset));
		in.seekg(-static_cast<int64_t>(offset + sizeof(offset)), std::ios::end);
		try
		{
			VM::Engine engine;
			BindHostCall(engine);
			engine.LoadProgram(in);
			result = engine.Run();
		}
		catch (VM::Exception& ex)
		{
			std::cout << ex.what() << std::endl;
			result = ex.ErrorCode();
		}
	}
	in.close();
	return result;
}

static void BindHostCall(VM::Engine& engine)
{
	engine.AppendHostCall(&WriteOutput);
	engine.AppendHostCall(&ReadInput);
	engine.AppendHostCall(&ValueToNumber);
	engine.AppendHostCall(&ValueToInteger);
	engine.AppendHostCall(&ValueToString);
	engine.AppendHostCall(&ValueFloor);
	engine.AppendHostCall(&ValueCeiling);
	engine.AppendHostCall(&GetArrayRow);
	engine.AppendHostCall(&GetArrayCol);
	engine.AppendHostCall(&GetRandom);
	engine.AppendHostCall(&XSetConsoleTitle);
	engine.AppendHostCall(&SetConsoleBackgroundColor);
	engine.AppendHostCall(&SetConsoleForegroundColor);
	engine.AppendHostCall(&XSetConsoleCursorPosition);
	engine.AppendHostCall(&ReadInputKey);
	engine.AppendHostCall(&ReadGVar);
	engine.AppendHostCall(&WriteGVar);
	engine.AppendHostCall(&ReadTimeMS);
	engine.AppendHostCall(&GetNewLine);
}


