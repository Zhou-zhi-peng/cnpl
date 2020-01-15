#include <cstdio>
#include <iostream>
#include <fstream>
#include <string>
#include <locale>
#include <codecvt>
#include <unistd.h>
#include <termio.h>
#include "../VM/VM.h"
#include "HostCalls.hpp"

static void BindHostCall(VM::Engine& engine);
static std::string GetExeFileName();

static const uint8_t byte_code_table[1024] __attribute__((used, section(".byte_code"))) = { 0xDE };


int main()
{
	int result = 32;
	std::fstream in;
	std::locale::global(std::locale(""));
	std::wcout.imbue(std::locale(""));
	auto moduleName = GetExeFileName();
	//moduleName = wstringToUtf8(L"/mnt/e/Work/myprojects/编译原理DEMO/test.linux.x64.out");
	int count = 0;
	do
	{
		count++;
		in.open(moduleName, std::ios::binary | std::ios::in);
		if (!in.good())
			usleep(1000 * 30);
	} while((!in.good()) && count<10);

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
	else
	{
		std::wcout << L"can not open byte code data." << std::endl;
	}
	in.close();
	return result;
}

static std::string GetExeFileName(void)
{
	char path[1024];
	ssize_t cnt = readlink("/proc/self/exe", path, 1024);
	if (cnt < 0 || cnt >= 1024)
	{
		return "";
	}
	return path;
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
