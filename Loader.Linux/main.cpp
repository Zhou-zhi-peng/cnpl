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
#ifdef BYTE_CODE_VM_LOADER
static std::string GetExeFileName();
#endif

static std::wstring utf8ToWstring(const std::string& str);

int main(int argc, char* args[])
{
	int result = 32;
	std::fstream in;
	std::locale::global(std::locale(""));
	std::wcout.imbue(std::locale(""));
#ifdef BYTE_CODE_VM_LOADER
	auto moduleName = GetExeFileName();
#else
	if (argc < 2)
	{
		std::cout << "The target program must be specified." << std::endl;
		return result;
	}
	std::string moduleName = args[1];
#endif
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
#ifdef BYTE_CODE_VM_LOADER
		uint64_t offset = 0;
		in.seekg(0, std::ios::end);
		in.seekg(-static_cast<int64_t>(sizeof(offset)), std::ios::cur);
		in.read((char*)(&offset), sizeof(offset));
		in.seekg(-static_cast<int64_t>(offset + sizeof(offset)), std::ios::end);
#endif
		try
		{
			VM::Engine engine;
			BindHostCall(engine);
			engine.LoadProgram(in);
			auto commandLineArgs = engine.GC().NewArrayValue(argc, 1);
			for (int i = 0; i < argc; i++)
				commandLineArgs->SetValue(i, 0, engine.GC().NewStringValue(utf8ToWstring(args[i])));
			engine.SetGlobalVariable(L"命令行参数", commandLineArgs);
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

#ifdef BYTE_CODE_VM_LOADER
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
#endif

static std::wstring utf8ToWstring(const std::string& str)
{
	std::wstring_convert< std::codecvt_utf8<wchar_t> > strCnv;
	return strCnv.from_bytes(str);
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
